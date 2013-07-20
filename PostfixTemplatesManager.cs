using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.Application;
using JetBrains.Application.Settings;
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

    private static TextRange GetTextRange(ITreeNode node, ReparsedCodeCompletionContext context = null)
    {
      if (context == null)
        return node.GetDocumentRange().TextRange;

      return context.ToDocumentRange(node.GetTreeTextRange());
    }

    [NotNull] public IList<ILookupItem> GetAvailableItems(
      [NotNull] ITreeNode node, bool looseChecks,
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
            var replaceRange = GetTextRange(referenceExpression, reparseContext);
            var expressionRange = GetTextRange(expression, reparseContext);
            var canBeStatement = CalculateCanBeStatement(referenceExpression);

            return CollectAvailableTemplates(
              referenceExpression, expression, replaceRange, expressionRange,
              canBeStatement, looseChecks, templateName);
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
              var canBeStatement = (expression.Parent == expressionStatement.Expression);
              var expressionRange = expression.GetDocumentRange().TextRange;
              var replaceRange = expressionRange.SetEndTo(node.GetDocumentRange().TextRange.EndOffset);

              return CollectAvailableTemplates(
                referenceExpr, expression, replaceRange, expressionRange,
                canBeStatement, looseChecks, templateName);
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
            if (wsToken != null && wsToken.GetTokenType() == CSharpTokenType.WHITE_SPACE) node = wsToken;

            expression = node.PrevSibling as ICSharpExpression; //todo: comments?
          }
        }

        // skip "expression statement is not closed with ';'" and friends
        while (last is IErrorElement) last = last.PrevSibling;
        if (last == null) break;
      }

      return expression;
    }

    private static bool CalculateCanBeStatement([NotNull] ICSharpExpression expression)
    {
      if (ExpressionStatementNavigator.GetByExpression(expression) != null)
        return true;

      // handle broken trees like: "lines.     \r\n   NextLineStatemement();"
      var containingStatement = expression.GetContainingNode<ICSharpStatement>();
      if (containingStatement != null)
      {
        var a = expression.GetTreeStartOffset();
        var b = containingStatement.GetTreeStartOffset();
        return (a == b);
      }

      return false;
    }

    [NotNull]
    private IList<ILookupItem> CollectAvailableTemplates(
      [NotNull] IReferenceExpression reference, [NotNull] ICSharpExpression expression,
      TextRange replaceRange, TextRange expressionRange, bool canBeStatement,
      bool forceMode, [CanBeNull] string templateName)
    {
      var acceptanceContext = new PostfixTemplateAcceptanceContext(
        reference, expression, replaceRange, expressionRange, canBeStatement, forceMode);

      var store = expression.GetSettingsStore();
      var settings = store.GetKey<PostfixCompletionSettings>(SettingsOptimization.OptimizeDefault);
      settings.DisabledProviders.SnapshotAndFreeze();

      var isTypeExpression = acceptanceContext.ReferencedElement is ITypeElement;
      var items = new List<ILookupItem>();

      foreach (var info in myTemplateProvidersInfos)
      {
        bool isEnabled;
        if (!settings.DisabledProviders.TryGet(info.SettingsKey, out isEnabled))
        {
          if (info.Metadata.DisabledByDefault) continue;
        }

        if (!isEnabled) continue; // check disabled providers

        if (templateName != null)
        {
          var templateNames = info.Metadata.TemplateNames;
          if (!templateNames.Contains(templateName, StringComparer.Ordinal))
            continue;
        }

        if (isTypeExpression && !info.Metadata.WorksOnTypes) continue;

        info.Provider.CreateItems(acceptanceContext, items);
      }

      if (templateName != null) // do not like it
        items.RemoveAll(x => x.Identity != templateName);

      return items;
    }
  }
}