using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.Application.DataContext;
using JetBrains.DataFlow;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.ControlFlow.PostfixCompletion.LookupItems;
using JetBrains.ReSharper.Feature.Services.Lookup;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Refactorings.IntroduceVariable;
using JetBrains.ReSharper.Refactorings.WorkflowNew;
using JetBrains.TextControl;

namespace JetBrains.ReSharper.ControlFlow.PostfixCompletion.TemplateProviders
{
  // todo: allow on types (FTW)

  [PostfixTemplateProvider(
    templateName: "var",
    description: "Introduces variable for expression",
    example: "var x = expr;")]
  public sealed class IntroduceVariableTemplateProvider : IPostfixTemplateProvider
  {
    public void CreateItems(PostfixTemplateAcceptanceContext context, ICollection<ILookupItem> consumer)
    {
      var contexts = new List<PrefixExpressionContext>();
      foreach (var expression in context.Expressions)
      {
        if (expression.Expression is IReferenceExpression)
        {
          // filter out too simple locals expressions
          var target = expression.ReferencedElement;
          if (target == null || target is IParameter || target is ILocalVariable) continue;
        }

        if (expression.Type.IsVoid()) continue;
        contexts.Add(expression);
      }

      if (contexts.Count == 0) return;

      var bestContext = contexts.FirstOrDefault(ctx => ctx.CanBeStatement)
                     ?? contexts.FirstOrDefault();
      if (bestContext == null) return;

      if (bestContext.CanBeStatement || context.ForceMode)
      {
        if (bestContext.CanBeStatement)
          consumer.Add(new StatementLookupItem(bestContext));
        else
          consumer.Add(new ExpressionLookupItem(bestContext));
      }
    }

    private sealed class ExpressionLookupItem : ExpressionPostfixLookupItem<ICSharpExpression>
    {
      public ExpressionLookupItem([NotNull] PrefixExpressionContext context)
        : base("var", context) { }

      protected override ICSharpExpression CreateExpression(
        CSharpElementFactory factory, ICSharpExpression expression)
      {
        return expression;
      }

      protected override void AfterComplete(
        ITextControl textControl, Suffix suffix, ICSharpExpression expression, int? caretPosition)
      {
        // note: yes, we are supressing suffix, since there is no nice way to preserve it
        ExecuteRefactoring(textControl, expression);
      }
    }

    private sealed class StatementLookupItem : StatementPostfixLookupItem<IExpressionStatement>
    {
      public StatementLookupItem([NotNull] PrefixExpressionContext context)
        : base("var", context) { }

      protected override IExpressionStatement CreateStatement(CSharpElementFactory factory)
      {
        return (IExpressionStatement) factory.CreateStatement("expr;");
      }

      protected override void PlaceExpression(
        IExpressionStatement statement, ICSharpExpression expression, CSharpElementFactory factory)
      {
        statement.Expression.ReplaceBy(expression);
      }

      protected override void AfterComplete(
        ITextControl textControl, Suffix suffix, IExpressionStatement statement, int? caretPosition)
      {
        ExecuteRefactoring(textControl, statement.Expression);
      }
    }

    private static void ExecuteRefactoring(
      [NotNull] ITextControl textControl, [NotNull] ICSharpExpression expression)
    {
      const string name = "IntroVariableAction";
      var solution = expression.GetSolution();
      var rules = DataRules
        .AddRule(name, ProjectModel.DataContext.DataConstants.SOLUTION, solution)
        .AddRule(name, DocumentModel.DataContext.DataConstants.DOCUMENT, textControl.Document)
        .AddRule(name, TextControl.DataContext.DataConstants.TEXT_CONTROL, textControl)
        .AddRule(name, Psi.Services.DataConstants.SELECTED_EXPRESSION, expression);

      Lifetimes.Using(lifetime =>
        WorkflowExecuter.ExecuteBatch(
          solution.GetComponent<DataContexts>().CreateWithDataRules(lifetime, rules),
          new IntroduceVariableWorkflow(solution, null)));
    }
  }
}