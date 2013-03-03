using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.Application;
using JetBrains.Application.Settings;
using JetBrains.ReSharper.ControlFlow.PostfixCompletion.LookupItems;
using JetBrains.ReSharper.ControlFlow.PostfixCompletion.Settings;
using JetBrains.ReSharper.Psi.CSharp.Parsing;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.ControlFlow.PostfixCompletion
{
  [ShellComponent]
  public class PostfixTemplatesManager
  {
    [NotNull] private readonly IEnumerable<IPostfixTemplateProvider> myTemplateProviders;

    public PostfixTemplatesManager(
      [NotNull] IEnumerable<IPostfixTemplateProvider> templateProviders)
    {
      myTemplateProviders = templateProviders;
      // todo: cache metadata
    }

    [NotNull]
    public IList<PostfixLookupItem> GetAvailableItems(
      [NotNull] ITreeNode node, bool looseChecks, string templateName = null)
    {
      if (node is ICSharpIdentifier || node is ITokenNode)
      {
        var referenceExpression = node.Parent as IReferenceExpression;
        if (referenceExpression != null)
        {
          var expression = referenceExpression.QualifierExpression;
          if (expression != null)
          {
            var replaceRange = referenceExpression.GetDocumentRange().TextRange;
            var canBeStatement = (ExpressionStatementNavigator.GetByExpression(referenceExpression) != null);

            return CollectAvailableTemplates(
              referenceExpression, expression, replaceRange,
              canBeStatement, looseChecks, templateName);
          }
        }

        // handle collisions with C# keyword names:
        // lines.foreach => ExpressionStatement(ReferenceExpression(lines.;Error));ForeachStatement(foreach;Error)
        // todo: prettify this shit

        var statement = node.Parent as ICSharpStatement;
        if (statement != null && statement.FirstChild == node && node.NextSibling is IErrorElement)
        {
          var expressionStatement = statement.PrevSibling as IExpressionStatement;
          if (expressionStatement != null)
          {
            ICSharpExpression expression = null;
            for (ITreeNode treeNode = expressionStatement, last; expression == null; treeNode = last)
            {
              last = treeNode.LastChild; // inspect all the last nodes traversing up tree
              if (last == null) break;

              if (last is IErrorElement && last.Parent is IReferenceExpression)
              {
                var prev = last.PrevSibling as ITokenNode;
                if (prev != null && prev.GetTokenType() == CSharpTokenType.DOT)
                  expression = prev.PrevSibling as ICSharpExpression;
              }

              // skip "expression statement is not closed with ';'" and friends
              while (last is IErrorElement) last = last.PrevSibling;
              if (last == null) break;
            }

            var re = ReferenceExpressionNavigator.GetByQualifierExpression(expression);
            if (expression != null && re != null)
            {
              var canBeStatement = (expression.Parent == expressionStatement.Expression);
              var replaceRange = expression.GetDocumentRange().TextRange.SetEndTo(
                                       node.GetDocumentRange().TextRange.EndOffset);

              return CollectAvailableTemplates(re, expression, replaceRange, canBeStatement, looseChecks, templateName);
            }
          }
        }
      }

      return EmptyList<PostfixLookupItem>.InstanceList;
    }

    [NotNull]
    private IList<PostfixLookupItem> CollectAvailableTemplates(
      [NotNull] IReferenceExpression referenceExpression, [NotNull] ICSharpExpression expression,
      TextRange replaceRange, bool canBeStatement, bool looseChecks, [CanBeNull] string templateName)
    {
      var qualifierType = expression.Type();
      var exprRange = expression.GetDocumentRange().TextRange;

      var acceptanceContext = new PostfixTemplateAcceptanceContext(
        referenceExpression, expression, qualifierType, canBeStatement, looseChecks);

      var store = expression.GetSettingsStore();
      var settings = store.GetKey<PostfixCompletionSettings>(SettingsOptimization.OptimizeDefault);
      settings.DisabledProviders.SnapshotAndFreeze();

      var items = new LocalList<PostfixLookupItem>();
      foreach (var provider in myTemplateProviders)
      {
        var providerType = provider.GetType();
        var providerKey = providerType.FullName;

        bool isEnabled;
        if (settings.DisabledProviders.TryGet(providerKey, out isEnabled) && !isEnabled)
          continue; // check disabled providers

        if (templateName != null)
        {
          // cache
          var attributes = (PostfixTemplateProviderAttribute[])
            providerType.GetCustomAttributes(typeof (PostfixTemplateProviderAttribute), false);
          if (attributes.Length != 1) continue;

          var templateNames = attributes[0].TemplateNames;
          if (!templateNames.Contains(templateName, StringComparer.Ordinal))
            continue;
        }

        foreach (var lookupItem in provider.CreateItems(acceptanceContext))
        {
          lookupItem.InitializeRanges(exprRange, replaceRange);
          items.Add(lookupItem);
        }
      }

      return items.ResultingList();
    }
  }
}