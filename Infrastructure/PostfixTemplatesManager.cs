using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.Application;
using JetBrains.Application.CommandProcessing;
using JetBrains.Application.PersistentMap;
using JetBrains.Application.Progress;
using JetBrains.Application.Settings;
using JetBrains.DocumentManagers;
using JetBrains.DocumentModel;
using JetBrains.DocumentModel.Transactions;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.ControlFlow.PostfixCompletion.Settings;
using JetBrains.ReSharper.Feature.Services.Lookup;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.ExtensionsAPI;
using JetBrains.ReSharper.Psi.Files;
using JetBrains.ReSharper.Psi.Services;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.ControlFlow.PostfixCompletion
{
  [ShellComponent]
  public class PostfixTemplatesManager
  {
    [NotNull] private readonly IList<TemplateProviderInfo> myTemplateProvidersInfos;

    public PostfixTemplatesManager([NotNull] IEnumerable<IPostfixTemplate> providers)
    {
      var infos = new List<TemplateProviderInfo>();
      foreach (var provider in providers)
      {
        var providerType = provider.GetType();
        var attributes = (PostfixTemplateAttribute[])
          providerType.GetCustomAttributes(
            typeof(PostfixTemplateAttribute), inherit: false);

        if (attributes.Length == 1)
        {
          var info = new TemplateProviderInfo(provider, providerType.FullName, attributes[0]);
          infos.Add(info);
        }
      }

      myTemplateProvidersInfos = infos.AsReadOnly();
    }

    public sealed class TemplateProviderInfo
    {
      [NotNull] public IPostfixTemplate Provider { get; private set; }
      [NotNull] public PostfixTemplateAttribute Metadata { get; private set; }
      [NotNull] public string SettingsKey { get; private set; }

      public TemplateProviderInfo([NotNull] IPostfixTemplate provider,
        [NotNull] string providerKey, [NotNull] PostfixTemplateAttribute metadata)
      {
        Provider = provider;
        Metadata = metadata;
        SettingsKey = providerKey;
      }
    }

    [NotNull] public IList<TemplateProviderInfo> TemplateProvidersInfos
    {
      get { return myTemplateProvidersInfos; }
    }

    [NotNull] public IList<ILookupItem> GetAvailableItems(
      [NotNull] ITreeNode node, [NotNull] PostfixExecutionContext context)
    {
      var postfixContext = IsAvailable(node, context);
      if (postfixContext == null || postfixContext.Expressions.Count == 0)
      {
        return EmptyList<ILookupItem>.InstanceList;
      }

      // todo: filter out namespaces

      var store = node.GetSettingsStore();
      var settings = store.GetKey<PostfixCompletionSettings>(SettingsOptimization.OptimizeDefault);
      settings.DisabledProviders.SnapshotAndFreeze();

      var isTypeExpression = postfixContext.InnerExpression.ReferencedElement is ITypeElement;
      var items = new List<ILookupItem>();

      //var templateName = context.SpecificTemplateName;
      foreach (var info in myTemplateProvidersInfos)
      {
        bool isEnabled;
        if (!settings.DisabledProviders.TryGet(info.SettingsKey, out isEnabled))
          isEnabled = !info.Metadata.DisabledByDefault;

        if (!isEnabled) continue; // check disabled providers

        //if (templateName != null)
        //{
        //  var name = info.Metadata.TemplateName;
        //  if (!string.Equals(templateName, name, StringComparison.Ordinal))
        //    continue;
        //}

        if (isTypeExpression && !info.Metadata.WorksOnTypes) continue;

        var lookupItem = info.Provider.CreateItems(postfixContext);
        if (lookupItem != null) items.Add(lookupItem);
      }

      //if (templateName != null) // do not like it
      //{
      //  for (var index = items.Count - 1; index >= 0; index--)
      //  {
      //    if (items[index].Identity == templateName)
      //      items.RemoveAt(index);
      //  }
      //}

      

      return items;
    }

    private sealed class ReferenceExpressionPostfixTemplateContext : PostfixTemplateContext
    {
      public ReferenceExpressionPostfixTemplateContext(
        [NotNull] IReferenceExpression reference, [NotNull] ICSharpExpression expression,
        [NotNull] PostfixExecutionContext executionContext)
        : base(reference, expression, executionContext) { }

      private static readonly string FixCommandName =
        typeof(ReferenceExpressionPostfixTemplateContext) + ".FixExpression";

      public override PrefixExpressionContext FixExpression(PrefixExpressionContext context)
      {
        var referenceExpression = (IReferenceExpression) Reference;

        var expression = context.Expression;
        if (expression.Parent == referenceExpression) // foo.bar => foo
        {
          ICSharpExpression newExpression = null;
          expression.GetPsiServices().DoTransaction(FixCommandName,
            () => newExpression = referenceExpression.ReplaceBy(expression));

          Assertion.AssertNotNull(newExpression, "newExpression != null");
          Assertion.Assert(newExpression.IsPhysical(), "newExpression.IsPhysical()");

          return new PrefixExpressionContext(this, newExpression);
        }

        if (expression.Contains(referenceExpression)) // boo > foo.bar => boo > foo
        {
          var qualifier = referenceExpression.QualifierExpression;
          expression.GetPsiServices().DoTransaction(FixCommandName,
            () => referenceExpression.ReplaceBy(qualifier.NotNull()));

          Assertion.Assert(expression.IsPhysical(), "expression.IsPhysical()");
        }

        return context;
      }
    }

    private sealed class ReferenceNamePostfixTemplateContext : PostfixTemplateContext
    {
      public ReferenceNamePostfixTemplateContext(
        [NotNull] IReferenceName reference, [NotNull] ICSharpExpression expression,
        [NotNull] PostfixExecutionContext executionContext)
        : base(reference, expression, executionContext) { }

      public override PrefixExpressionContext FixExpression(PrefixExpressionContext context)
      {
        var referenceName = (IReferenceName) Reference;

        var expression = context.Expression;
        if (expression.Contains(referenceName)) // x is T.bar => x is T
        {
          var qualifier = referenceName.Qualifier.NotNull();
          var newExpression = referenceName.ReplaceBy(qualifier);
        }

        return context;
      }
    }

    private sealed class BrokenStatementPostfixTemplateContext : PostfixTemplateContext
    {
      public BrokenStatementPostfixTemplateContext(
        [NotNull] ITreeNode reference, [NotNull] ICSharpExpression expression,
        [NotNull] PostfixExecutionContext executionContext)
        : base(reference, expression, executionContext) { }

      private static readonly string FixCommandName =
        typeof(BrokenStatementPostfixTemplateContext) + ".FixExpression";

      public override PrefixExpressionContext FixExpression(PrefixExpressionContext context)
      {
        var expressionRange = ExecutionContext.GetDocumentRange(context.Expression);
        var referenceRange = ExecutionContext.GetDocumentRange(Reference);

        var text = expressionRange.SetEndTo(referenceRange.TextRange.EndOffset).GetText();
        var indexOfReferenceDot = text.IndexOf('.');
        if (indexOfReferenceDot <= 0) return context;

        var realReferenceRange = referenceRange.SetStartTo(
          expressionRange.TextRange.StartOffset + indexOfReferenceDot);

        var solution = ExecutionContext.PsiModule.GetSolution();
        var document = context.Expression.GetDocumentRange().Document;

        using (solution.CreateTransactionCookie(
          DefaultAction.Commit, FixCommandName, NullProgressIndicator.Instance))
        {
          document.ReplaceText(realReferenceRange.TextRange, ")");
          document.InsertText(expressionRange.TextRange.StartOffset, "unchecked(");
        }

        solution.GetPsiServices().CommitAllDocuments();

        var uncheckedExpression = TextControlToPsi.GetElement<IUncheckedExpression>(
          solution, document, expressionRange.TextRange.StartOffset + 1);
        if (uncheckedExpression != null)
        {
          var operand = uncheckedExpression.Operand;
          solution.GetPsiServices().DoTransaction(FixCommandName, () =>
          {
            LowLevelModificationUtil.DeleteChild(operand);
            LowLevelModificationUtil.ReplaceChildRange(
              uncheckedExpression, uncheckedExpression, operand); 
          });

          Assertion.Assert(operand.IsPhysical(), "operand.IsPhysical()");
          return new PrefixExpressionContext(this, operand);
        }

        return context;
      }
    }

    [CanBeNull] public PostfixTemplateContext IsAvailable(
      [NotNull] ITreeNode position, [NotNull] PostfixExecutionContext executionContext)
    {
      if (!(position is ICSharpIdentifier)) return null;

      // expr.__
      var referenceExpression = position.Parent as IReferenceExpression;
      if (referenceExpression != null && referenceExpression.Delimiter != null)
      {
        var expression = referenceExpression.QualifierExpression;
        if (expression != null)
        {
          return new ReferenceExpressionPostfixTemplateContext(referenceExpression, expression, executionContext);
        }
      }

      // String.__
      var referenceName = position.Parent as IReferenceName;
      if (referenceName != null && referenceName.Qualifier != null && referenceName.Delimiter != null)
      {
        var typeUsage = referenceName.Parent as ITypeUsage;
        if (typeUsage != null)
        {
          var expression = typeUsage.Parent as ICSharpExpression;
          if (expression != null)
          {
            return new ReferenceNamePostfixTemplateContext(referenceName, expression, executionContext);
          }
        }
      }

      // string.__
      var brokenStatement = FindBrokenStatement(position);
      if (brokenStatement != null)
      {
        var expressionStatement = brokenStatement.PrevSibling as IExpressionStatement;
        if (expressionStatement != null)
        {
          ITreeNode coolNode;
          var expression = FindExpressionBrokenByKeyword2(expressionStatement, out coolNode);
          if (expression != null)
          {
            return new BrokenStatementPostfixTemplateContext(brokenStatement, expression, executionContext);
          }
        }
      }

      return null;
    }

    [CanBeNull]
    private static ICSharpStatement FindBrokenStatement([NotNull] ITreeNode node)
    {
      var parent = node.Parent as ICSharpStatement;
      if (parent != null && parent.FirstChild == node && node.NextSibling is IErrorElement)
      {
        return parent;
      }

      var refExpr = node.Parent as IReferenceExpression;
      if (refExpr != null && refExpr.FirstChild == node)
      {
        if (refExpr.NextSibling is IErrorElement || !refExpr.IsPhysical())
        {
          return refExpr.Parent as IExpressionStatement;
        }
      }

      return null;
    }

    [CanBeNull]
    private static ICSharpExpression FindExpressionBrokenByKeyword2(
      [NotNull] IExpressionStatement statement, out ITreeNode coolNode)
    {
      ICSharpExpression expression = null;
      var errorFound = false;
      coolNode = null;

      for (ITreeNode treeNode = statement, last;; treeNode = last)
      {
        last = treeNode.LastChild; // inspect all the last nodes traversing up tree
        if (last == null) break;

        if (!errorFound) errorFound = last is IErrorElement;
        if (errorFound && last.Parent is ITypeUsage)
        {
          if (last is IReferenceName && last.Parent is IUserTypeUsage)
            coolNode = last;

          expression = last.Parent.Parent as ICSharpExpression;
          break;
        }

        // skip "expression statement is not closed with ';'" and friends
        while (last is IErrorElement)
        {
          coolNode = last;
          last = last.PrevSibling;
        }

        if (last == null) break;
      }

      return expression;
    }
  }
}