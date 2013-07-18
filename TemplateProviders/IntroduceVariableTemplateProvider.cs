using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.Application;
using JetBrains.Application.DataContext;
using JetBrains.DataFlow;
using JetBrains.ReSharper.ControlFlow.PostfixCompletion.LookupItems;
using JetBrains.ReSharper.Feature.Services.Lookup;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Refactorings.IntroduceVariable;
using JetBrains.ReSharper.Refactorings.WorkflowNew;
using JetBrains.TextControl;
using DataConstants = JetBrains.DocumentModel.DataContext.DataConstants;

namespace JetBrains.ReSharper.ControlFlow.PostfixCompletion.TemplateProviders
{
  [PostfixTemplateProvider("var", "Introduces variable for expression")]
  public class IntroduceVariableTemplateProvider : IPostfixTemplateProvider
  {
    public void CreateItems(PostfixTemplateAcceptanceContext context, ICollection<ILookupItem> consumer)
    {
      // todo: what if inside var decl = ..here..?

      var contexts = new List<PrefixExpressionContext>();
      foreach (var expression in context.PossibleExpressions)
      {
        if (expression.Expression is IReferenceExpression)
        {
          // filter out too simple locals expressions
          var target = expression.ReferencedElement;
          if (target == null || target is IParameter || target is ILocalVariable)
            continue;
        }

        if (expression.ExpressionType.IsVoid())
        {
          continue;
        }

        contexts.Add(expression);
      }

      if (contexts.Count == 0) return;

      var a = contexts.FirstOrDefault(x => x.CanBeStatement) ?? contexts.FirstOrDefault();
      if (a != null)
      {
        var canBeStatement = a.CanBeStatement;
        if (canBeStatement || context.ForceMode)
        {
          //consumer.Add(canBeStatement
          //  ? (ILookupItem)new IntroduceVariableLookupItem(a)
          //  : new IntroduceVariableLookupItem2(a));

          consumer.Add(new IntroduceVariableLookupItem2(a));
        }
      }
    }

    private class IntroduceVariableLookupItem : StatementPostfixLookupItem<IExpressionStatement>
    {
      public IntroduceVariableLookupItem([NotNull] PrefixExpressionContext expression)
        : base("var", expression) { }

      protected override IExpressionStatement CreateStatement(IPsiModule psiModule, CSharpElementFactory factory)
      {
        return (IExpressionStatement) factory.CreateStatement(PostfixMarker + ";");
      }

      protected override void PutExpression(IExpressionStatement statement, ICSharpExpression expression)
      {
        statement.Expression.ReplaceBy(expression);
      }

      protected override void AfterComplete(
        ITextControl textControl, Suffix suffix, IExpressionStatement statement, int? caretPosition)
      {
        // todo: suffix?

        if (statement != null)
        {
          // set selection for introduce viriable
          // todo: do we need this?
          //textControl.Selection.SetRanges(new[] {
          //  TextControlPosRange.FromDocRange(
          //    textControl, expressionStatement.Expression.GetDocumentRange().TextRange)
          //});

          const string name = "IntroVariableAction";
          var solution = statement.GetSolution();
          var rules = DataRules
            .AddRule(name, ProjectModel.DataContext.DataConstants.SOLUTION, solution)
            .AddRule(name, DataConstants.DOCUMENT, textControl.Document)
            .AddRule(name, TextControl.DataContext.DataConstants.TEXT_CONTROL, textControl)
            .AddRule(name, Psi.Services.DataConstants.SELECTED_EXPRESSION, statement.Expression);

          Lifetimes.Using(lifetime =>
            WorkflowExecuter.ExecuteBatch(
              Shell.Instance.GetComponent<DataContexts>().CreateWithDataRules(lifetime, rules),
              new IntroduceVariableWorkflow(solution, null)));
        }
      }
    }

    private class IntroduceVariableLookupItem2 : ExpressionPostfixLookupItem<ICSharpExpression>
    {
      public IntroduceVariableLookupItem2([NotNull] PrefixExpressionContext expression)
        : base("var", expression) { }

      protected override ICSharpExpression CreateExpression(
        IPsiModule psiModule, CSharpElementFactory factory, ICSharpExpression expression)
      {
        return expression;
      }

      protected override void AfterComplete(
        ITextControl textControl, Suffix suffix, ICSharpExpression expression, int? caretPosition)
      {
        // todo: suffix?

        if (expression != null)
        {
          // set selection for introduce viriable
          // todo: do we need this?
          //textControl.Selection.SetRanges(new[] {
          //  TextControlPosRange.FromDocRange(
          //    textControl, expressionStatement.Expression.GetDocumentRange().TextRange)
          //});

          const string name = "IntroVariableAction";
          var solution = expression.GetSolution();
          var rules = DataRules
            .AddRule(name, ProjectModel.DataContext.DataConstants.SOLUTION, solution)
            .AddRule(name, DataConstants.DOCUMENT, textControl.Document)
            .AddRule(name, TextControl.DataContext.DataConstants.TEXT_CONTROL, textControl)
            .AddRule(name, Psi.Services.DataConstants.SELECTED_EXPRESSION, expression);

          Lifetimes.Using(lifetime =>
            WorkflowExecuter.ExecuteBatch(
              Shell.Instance.GetComponent<DataContexts>().CreateWithDataRules(lifetime, rules),
              new IntroduceVariableWorkflow(solution, null)));
        }
      }
    }
  }
}