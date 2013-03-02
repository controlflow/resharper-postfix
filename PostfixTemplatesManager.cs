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
    public IList<PostfixLookupItem> GetAvailableItems([NotNull] ITreeNode node, string templateName = null)
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
            return CollectAvailableTemplates(expression, replaceRange, canBeStatement, templateName);
          }
        }

        // handle collisions with C# keyword names:
        // lines.foreach => ExpressionStatement(ReferenceExpression(lines.;Error));ForeachStatement(foreach;Error)
        // todo: prettify this shit

        var statement = node.Parent as ICSharpStatement;
        if (statement != null && node.NextSibling is IErrorElement)
        {
          var expressionStatement = statement.PrevSibling as IExpressionStatement;
          if (expressionStatement != null)
          {
            bool canBeStatement = true;
            ICSharpExpression expr = null;

            ITreeNode node1 = expressionStatement;
            while (true)
            {
              var last = node1.LastChild;
              if (last == null) break;

              var re = last.Parent as IReferenceExpression;
              if (re != null)
              {
                var prev = last.PrevSibling as ITokenNode;
                if (last is IErrorElement && prev != null && prev.GetTokenType() == CSharpTokenType.DOT)
                {
                  var expre = prev.PrevSibling as ICSharpExpression;
                  if (expre != null)
                  {
                    expr = expre;


                    if (re != expressionStatement.Expression)
                    {
                      canBeStatement = false;
                    }

                    // FML!!!!!
                    break;
                  }
                }
              }

              while (last is IErrorElement)
                last = last.PrevSibling;

              if (last == null) break;
              node1 = last;
            }

            //var expression = expr as IReferenceExpression;
            //var canBe = (expression != null && expression.LastChild is IErrorElement);

            if (expr != null)
            {
              var replaceRange =
                expr.GetDocumentRange().TextRange.SetEndTo(
                node.GetDocumentRange().TextRange.EndOffset);

              return CollectAvailableTemplates(expr, replaceRange, canBeStatement, templateName);
            }
          }
        }
      }

      return EmptyList<PostfixLookupItem>.InstanceList;
    }

    [NotNull]
    private IList<PostfixLookupItem> CollectAvailableTemplates(
      [NotNull] ICSharpExpression expression, TextRange replaceRange,
      bool canBeStatement, [CanBeNull] string templateName)
    {
      var qualifierType = expression.Type();
      var exprRange = expression.GetDocumentRange().TextRange;

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

        foreach (var lookupItem in provider.CreateItems(expression, qualifierType, canBeStatement))
        {
          lookupItem.InitializeRanges(exprRange, replaceRange);
          items.Add(lookupItem);
        }
      }

      return items.ResultingList();
    }
  }
}