using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.Application.Settings;
using JetBrains.ReSharper.ControlFlow.PostfixCompletion.LookupItems;
using JetBrains.ReSharper.ControlFlow.PostfixCompletion.Settings;
using JetBrains.ReSharper.Feature.Services.Lookup;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Modules;

namespace JetBrains.ReSharper.ControlFlow.PostfixCompletion.TemplateProviders
{
  [PostfixTemplateProvider("if", "Checks boolean expression to be 'true'")]
  public class IfStatementTemplateProvider2 : IPostfixTemplateProvider
  {
    public void CreateItems(PostfixTemplateAcceptanceContext context, ICollection<ILookupItem> consumer)
    {
      foreach (var expression in context.PossibleExpressions)
      {
        if (expression.CanBeStatement)
        {
          if (expression.ExpressionType.IsBool() || context.LooseChecks ||
              expression.Expression is IRelationalExpression)
          {
            consumer.Add(new IfStatementPostfixLookupItem("if", context, expression));
          }
        }
      }
    }

    private sealed class IfStatementPostfixLookupItem : PostfixStatementLookupItem<IIfStatement>
    {
      public IfStatementPostfixLookupItem([NotNull] string shortcut,
        [NotNull] PostfixTemplateAcceptanceContext context,
        [NotNull] PrefixExpressionContext expression)
        : base(shortcut, context, expression) { }

      protected override IIfStatement CreateStatement(
        IPsiModule psiModule, IContextBoundSettingsStore settings, CSharpElementFactory factory)
      {
        if (settings.GetValue(PostfixCompletionSettingsAccessor.UseBracesForEmbeddedStatements))
        {
          return (IIfStatement)factory.CreateStatement("if (expr){" + CaretMarker + ";}");
        }

        return (IIfStatement)factory.CreateStatement("if (expr)" + CaretMarker + ";");
      }

      protected override void PutExpression(IIfStatement statement, ICSharpExpression expression)
      {
        statement.Condition.ReplaceBy(expression);
      }
    }
  }
}