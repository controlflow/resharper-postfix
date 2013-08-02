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
      [NotNull] ITreeNode node, bool forceMode,
      [CanBeNull] ReparsedCodeCompletionContext reparseContext = null,
      [CanBeNull] string templateName = null)
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
              reparseContext, forceMode, templateName);
          }
        }

        // handle collisions with C# keyword names 'lines.foreach' =>
        // ExpressionStatement(ReferenceExpression(lines.;Error);Error);ForeachStatement(foreach;Error)
        var statement = node.Parent as ICSharpStatement;
        if (statement != null && statement.FirstChild == node &&
            node.NextSibling is IErrorElement && reparseContext == null)
        {
          // todo: more constrained check? what to remove?

          var expressionStatement = statement.PrevSibling as IExpressionStatement;
          if (expressionStatement != null)
          {
            var expression = FindExpressionBrokenByKeyword(expressionStatement);
            var referenceExpr = ReferenceExpressionNavigator.GetByQualifierExpression(expression);
            if (expression != null && referenceExpr != null)
            {
              var expressionRange = expression.GetDocumentRange();
              var replaceRange = expressionRange.SetEndTo(node.GetDocumentRange().TextRange.EndOffset);

              return CollectAvailableTemplates(
                referenceExpr, expression, replaceRange,
                null, forceMode, templateName);
            }
          }
        }

        // handle collisions with predefined types 'x as string.'
        if (node.Parent is IErrorElement &&
            node.Parent.PrevSibling is ITypeUsage)
        {
          var expression = node.Parent.Parent as ICSharpExpression;
          if (expression != null)
          {
            var a = ToDocumentRange(reparseContext, node.Parent.PrevSibling);
            var b = ToDocumentRange(reparseContext, node);
            var c = a.SetEndTo(b.TextRange.EndOffset);

            return CollectAvailableTemplates(
              node.Parent.PrevSibling, expression, c,
              reparseContext, forceMode, templateName);
          }
        }

        // handle collisions with user types 'x as String.'
        var referenceName = node.Parent as IReferenceName;
        if (referenceName != null && referenceName.Qualifier != null && referenceName.Delimiter != null)
        {
          var usage = referenceName.Parent as ITypeUsage;
          if (usage != null)
          {
            // wrong, type usage
            var expression = usage.Parent as ICSharpExpression;
            if (expression != null)
            {
              var a = ToDocumentRange(reparseContext, referenceName.Qualifier);
              var b = ToDocumentRange(reparseContext, referenceName.Delimiter);
              var c = a.SetEndTo(b.TextRange.EndOffset);

              return CollectAvailableTemplates(
                referenceName, expression, c,
                reparseContext, forceMode, templateName);
            }
          }
        }
      }

      return EmptyList<ILookupItem>.InstanceList;
    }

    public DocumentRange ToDocumentRange(
      [CanBeNull] ReparsedCodeCompletionContext reparsedContext, [NotNull] ITreeNode treeNode)
    {
      var documentRange = treeNode.GetDocumentRange();
      if (reparsedContext == null) return documentRange;

      var originalRange = reparsedContext.ToDocumentRange(treeNode.GetTreeTextRange());
      return new DocumentRange(documentRange.Document, originalRange);
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

    [NotNull]
    private IList<ILookupItem> CollectAvailableTemplates(
      [NotNull] ITreeNode reference, [NotNull] ICSharpExpression expression,
      DocumentRange replaceRange, [CanBeNull] ReparsedCodeCompletionContext context,
      bool forceMode, [CanBeNull] string templateName)
    {
      var postfixContext = new PostfixTemplateAcceptanceContext(
        reference, expression, replaceRange, context, forceMode);

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