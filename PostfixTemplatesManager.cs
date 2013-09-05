using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.Application;
using JetBrains.Application.Settings;
using JetBrains.DocumentModel;
using JetBrains.ReSharper.ControlFlow.PostfixCompletion.Settings;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure;
using JetBrains.ReSharper.Feature.Services.Lookup;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Parsing;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.ControlFlow.PostfixCompletion
{
  [ShellComponent]
  public class PostfixTemplatesManager
  {
    [NotNull] private readonly IList<TemplateProviderInfo> myTemplateProvidersInfos;

    public PostfixTemplatesManager([NotNull] IEnumerable<IPostfixTemplateProvider> providers)
    {
      var infos = new List<TemplateProviderInfo>();
      foreach (var provider in providers)
      {
        var providerType = provider.GetType();
        var attributes = (PostfixTemplateProviderAttribute[])
          providerType.GetCustomAttributes(typeof (PostfixTemplateProviderAttribute), false);
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
      [NotNull] public IPostfixTemplateProvider Provider { get; private set; }
      [NotNull] public PostfixTemplateProviderAttribute Metadata { get; private set; }
      [NotNull] public string SettingsKey { get; private set; }

      public TemplateProviderInfo([NotNull] IPostfixTemplateProvider provider,
        [NotNull] string providerKey, [NotNull] PostfixTemplateProviderAttribute metadata)
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
      [NotNull] ITreeNode node, bool forceMode, [NotNull] ILookupItemsOwner lookupItemsOwner,
      [CanBeNull] ReparsedCodeCompletionContext reparseContext, [CanBeNull] string templateName)
    {
      if (node is ICSharpIdentifier || node is ITokenNode)
      {
        // simple case: 'anyExpr.notKeyworkShortcut'
        var referenceExpression = node.Parent as IReferenceExpression;
        if (referenceExpression != null)
        {
          var expression = referenceExpression.QualifierExpression;
          if (expression != null)
          {
            return CollectAvailableTemplates(
              referenceExpression, expression, DocumentRange.InvalidRange,
              reparseContext, forceMode, templateName, lookupItemsOwner);
          }
        }

        // handle collisions with C# keyword names 'lines.foreach' =>
        // ExpressionStatement(ReferenceExpression(lines.;Error);Error);ForeachStatement(foreach;Error)
        var statement = node.Parent as ICSharpStatement;
        if (statement != null && statement.FirstChild == node && reparseContext == null)
        {
          var expressionStatement = statement.PrevSibling as IExpressionStatement;
          if (expressionStatement != null)
          {
            var expression = FindExpressionBrokenByKeyword(expressionStatement);
            var referenceExpr = ReferenceExpressionNavigator.GetByQualifierExpression(expression);
            if (expression != null && referenceExpr != null)
            {
              var expressionRange = expression.GetDocumentRange();
              var nodeRange = node.GetDocumentRange().TextRange;
              var replaceRange = expressionRange.SetEndTo(nodeRange.EndOffset);

              return CollectAvailableTemplates(
                referenceExpr, expression, replaceRange, null,
                forceMode, templateName, lookupItemsOwner);
            }
          }
        }

        // handle collisions with predefined types 'x as string.'
        if (node.Parent is IErrorElement)
        {
          var typeUsage = node.Parent.PrevSibling as ITypeUsage;
          if (typeUsage != null)
          {
            var expression = node.Parent.Parent as ICSharpExpression;
            if (expression != null)
            {
              var typeUsageRange = reparseContext.ToDocumentRange(typeUsage);
              var nodeRange = reparseContext.ToDocumentRange(node).TextRange;
              var replaceRange = typeUsageRange.SetEndTo(nodeRange.EndOffset);

              return CollectAvailableTemplates(
                typeUsage, expression, replaceRange, reparseContext,
                forceMode, templateName, lookupItemsOwner);
            }
          }
        }

        // handle collisions with user types 'x as String.'
        var referenceName = node.Parent as IReferenceName;
        if (referenceName != null &&
            referenceName.Qualifier != null &&
            referenceName.Delimiter != null)
        {
          var usage = referenceName.Parent as ITypeUsage;
          if (usage != null)
          {
            var expression = usage.Parent as ICSharpExpression;
            if (expression != null)
            {
              var qualifierRange = reparseContext.ToDocumentRange(referenceName.Qualifier);
              var delimiterRange = reparseContext.ToDocumentRange(referenceName.Delimiter);
              var replaceRange = qualifierRange.SetEndTo(delimiterRange.TextRange.EndOffset);

              return CollectAvailableTemplates(
                referenceName, expression, replaceRange, reparseContext,
                forceMode, templateName, lookupItemsOwner);
            }
          }
        }

        ICSharpStatement statement1 = null;

        if (node.Parent is ICSharpStatement &&
          node.Parent.FirstChild == node &&
          node.NextSibling is IErrorElement)
        {
          statement1 = (ICSharpStatement) node.Parent;
        }

        if (statement1 == null)
        {
          var re = node.Parent as IReferenceExpression;
          if (re != null && re.FirstChild == node && re.NextSibling is IErrorElement)
          {
            statement1 = re.Parent as IExpressionStatement;
          }
        }

        if (statement1 != null && reparseContext == null)
        {
          // todo: more constrained check? what to remove?

          var expressionStatement = statement1.PrevSibling as IExpressionStatement;
          if (expressionStatement != null)
          {
            ITreeNode coolNode;
            var expression = FindExpressionBrokenByKeyword1(expressionStatement, out coolNode);
            if (expression != null)
            {
              var expressionRange = expression.GetDocumentRange();
              var nodeRange = node.GetDocumentRange().TextRange;
              var replaceRange = expressionRange.SetEndTo(nodeRange.EndOffset);

              return CollectAvailableTemplates(
                coolNode /* ? */, expression, replaceRange, null,
                forceMode, templateName, lookupItemsOwner);
            }
          }
        }
      }

      return EmptyList<ILookupItem>.InstanceList;
    }

    [CanBeNull]
    private static ICSharpExpression FindExpressionBrokenByKeyword([NotNull] IExpressionStatement statement)
    {
      ICSharpExpression expression = null;
      var errorFound = false;

      for (ITreeNode treeNode = statement, last; expression == null; treeNode = last)
      {
        last = treeNode.LastChild; // inspect all the last nodes traversing up tree
        if (last == null) break;

        if (!errorFound) errorFound = last is IErrorElement;

        if (errorFound && last.Parent is IReferenceExpression)
        {
          var dot = (last is IErrorElement ? last.PrevSibling : last) as ITokenNode;
          if (dot != null && dot.GetTokenType() == CSharpTokenType.DOT)
          {
            ITreeNode node = dot;

            // skip whitespace in case of "expr   .if"
            var wsToken = dot.PrevSibling as ITokenNode;
            if (wsToken != null && wsToken.GetTokenType() == CSharpTokenType.WHITE_SPACE)
              node = wsToken;

            expression = node.PrevSibling as ICSharpExpression; //todo: comments?
          }
        }

        // skip "expression statement is not closed with ';'" and friends
        while (last is IErrorElement) last = last.PrevSibling;
        if (last == null) break;
      }

      return expression;
    }

    [CanBeNull]
    private static ICSharpExpression FindExpressionBrokenByKeyword1(
      [NotNull] IExpressionStatement statement, out ITreeNode coolNode)
    {
      ICSharpExpression expression = null;
      var errorFound = false;
      coolNode = null;

      // todo: missing dot check

      for (ITreeNode treeNode = statement, last; expression == null; treeNode = last)
      {
        last = treeNode.LastChild; // inspect all the last nodes traversing up tree
        if (last == null) break;

        if (!errorFound) errorFound = last is IErrorElement;

        if (errorFound && last.Parent is ITypeUsage)
        {
          if (last is IReferenceName && last.Parent is IUserTypeUsage)
          {
            coolNode = last;
          }

          // mmmm
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

    [NotNull]
    private IList<ILookupItem> CollectAvailableTemplates(
      [NotNull] ITreeNode reference, [NotNull] ICSharpExpression expression,
      DocumentRange replaceRange, [CanBeNull] ReparsedCodeCompletionContext context,
      bool forceMode, [CanBeNull] string templateName, [NotNull] ILookupItemsOwner itemsOwner)
    {
      var postfixContext = new PostfixTemplateAcceptanceContext(
        reference, expression, replaceRange, context, forceMode, itemsOwner);

      var store = expression.GetSettingsStore();
      var settings = store.GetKey<PostfixCompletionSettings>(SettingsOptimization.OptimizeDefault);
      settings.DisabledProviders.SnapshotAndFreeze();

      var isTypeExpression = postfixContext.InnerExpression.ReferencedElement is ITypeElement;
      var items = new List<ILookupItem>();

      foreach (var info in myTemplateProvidersInfos)
      {
        bool isEnabled;
        if (!settings.DisabledProviders.TryGet(info.SettingsKey, out isEnabled))
          isEnabled = !info.Metadata.DisabledByDefault;

        if (!isEnabled) continue; // check disabled providers

        if (templateName != null)
        {
          var templateNames = info.Metadata.TemplateNames;
          if (!templateNames.Contains(templateName, StringComparer.Ordinal))
            continue;
        }

        if (isTypeExpression && !info.Metadata.WorksOnTypes) continue;

        info.Provider.CreateItems(postfixContext, items);
      }

      if (templateName != null) // do not like it
        items.RemoveAll(x => x.Identity != templateName);

      return items;
    }
  }
}