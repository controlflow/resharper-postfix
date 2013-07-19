using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.Application.Settings;
using JetBrains.ReSharper.ControlFlow.PostfixCompletion.LookupItems;
using JetBrains.ReSharper.ControlFlow.PostfixCompletion.Settings;
using JetBrains.ReSharper.Feature.Services.Lookup;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.CSharp.Util;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.TextControl;
using JetBrains.Util;

namespace JetBrains.ReSharper.ControlFlow.PostfixCompletion.TemplateProviders
{
  [PostfixTemplateProvider("ifnot", "Checks boolean expression to be 'false'")]
  public class IfNotStatementTemplateProvider : IPostfixTemplateProvider
  {
    public void CreateItems(PostfixTemplateAcceptanceContext context, ICollection<ILookupItem> consumer)
    {
      foreach (var expression in context.PossibleExpressions)
      {
        if (!expression.CanBeStatement) continue;

        if (context.ForceMode ||
            expression.ExpressionType.IsBool() ||
            expression.Expression is IRelationalExpression)
        {
          var bracesInsertion = context.SettingsStore.GetValue(
            PostfixCompletionSettingsAccessor.UseBracesForEmbeddedStatements);
          consumer.Add(new LookupItem(expression, bracesInsertion));
          break;
        }
      }
    }

    private sealed class LookupItem : StatementPostfixLookupItem<IIfStatement>
    {
      public LookupItem(
        [NotNull] PrefixExpressionContext context, bool bracesInsertion)
        : base("ifnot", context)
      {
        BracesInsertion = bracesInsertion;
      }

      private bool BracesInsertion { get; set; }

      protected override IIfStatement CreateStatement(
        IPsiModule psiModule, CSharpElementFactory factory)
      {
        var template = BracesInsertion
          ? "if (expr){" + CaretMarker + ";}"
          : "if (expr)" + CaretMarker + ";";

        return (IIfStatement) factory.CreateStatement(template);
      }

      protected override void PutExpression(IIfStatement statement, ICSharpExpression expression)
      {
        var physical = statement.Condition.ReplaceBy(expression);
        var negated = CSharpExpressionUtil.CreateLogicallyNegatedExpression(physical);
        statement.Condition.ReplaceBy(negated.NotNull());
      }

      protected override void ReplaySuffix(ITextControl textControl, Suffix suffix)
      {
        if (BracesInsertion && suffix.HasPresentation && suffix.Presentation == '{')
          return;

        base.ReplaySuffix(textControl, suffix);
      }
    }
  }
}