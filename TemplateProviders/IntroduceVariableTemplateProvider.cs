using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.Application;
using JetBrains.Application.DataContext;
using JetBrains.DataFlow;
using JetBrains.DocumentModel;
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
using JetBrains.Util;
using JetBrains.Util.EventBus;

namespace JetBrains.ReSharper.ControlFlow.PostfixCompletion.TemplateProviders
{
  [PostfixTemplateProvider(
    templateName: "var",
    description: "Introduces variable for expression",
    example: "var x = expr;", WorksOnTypes = true)]
  public sealed class IntroduceVariableTemplateProvider : IPostfixTemplateProvider
  {
    public void CreateItems(
      PostfixTemplateAcceptanceContext context, ICollection<ILookupItem> consumer)
    {
      var contexts = new List<PrefixExpressionContext>();
      foreach (var exprContext in context.Expressions)
      {
        if (exprContext.Expression is IReferenceExpression)
        {
          // filter out too simple locals expressions
          var target = exprContext.ReferencedElement;
          if (target == null || target is IParameter || target is ILocalVariable)
            continue;
        }

        if (exprContext.Type.IsVoid()) continue;

        if (exprContext.ReferencedType != null)
        {
          if (!exprContext.CanBeStatement) continue;

          if (TypeUtils.IsInstantiable(
            exprContext.ReferencedType.GetTypeElement().NotNull(),
            exprContext.Expression) == 0) continue;
        }

        contexts.Add(exprContext);
      }

      if (contexts.Count == 0) return;

      var bestContext = contexts.FirstOrDefault(ctx => ctx.CanBeStatement)
                        ?? contexts.FirstOrDefault();
      if (bestContext == null) return;

      if (bestContext.CanBeStatement)
      {
        if (bestContext.ReferencedType != null)
          consumer.Add(new Statement2LookupItem(bestContext, bestContext.ReferencedType));
        else
          consumer.Add(new StatementLookupItem(bestContext));
      }
      else if (context.ForceMode)
      {
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

    private sealed class Statement2LookupItem : StatementPostfixLookupItem<IExpressionStatement>
    {
      private readonly IDeclaredType myReferencedType;

      public Statement2LookupItem([NotNull] PrefixExpressionContext context, IDeclaredType referencedType)
        : base("var", context)
      {
        myReferencedType = referencedType;
      }

      protected override IExpressionStatement CreateStatement(CSharpElementFactory factory)
      {
        return (IExpressionStatement) factory.CreateStatement("new $0();", myReferencedType);
      }

      protected override void PlaceExpression(
        IExpressionStatement statement, ICSharpExpression expression, CSharpElementFactory factory)
      {
        //var oc = (IObjectCreationExpression) statement.Expression;
        //oc.SetTypeName(factory.CreateReferenceName("$0"))

        //statement.Expression.ReplaceBy(expression);
      }

      protected override void AfterComplete(
        ITextControl textControl, Suffix suffix, IExpressionStatement statement, int? caretPosition)
      {
        var oc = (IObjectCreationExpression)statement.Expression;
        var eo = oc.LPar.GetDocumentRange().CreateRangeMarker();
        //textControl.Caret.MoveTo(eo, CaretVisualPlacement.DontScrollIfVisible);

        ExecuteRefactoring(textControl, statement.Expression, eo);
      }
    }

    private static void ExecuteRefactoring(
      [NotNull] ITextControl textControl, [NotNull] ICSharpExpression expression,
      IRangeMarker rm = null)
    {
      const string name = "IntroVariableAction";
      var solution = expression.GetSolution();
      var rules = DataRules
        .AddRule(name, ProjectModel.DataContext.DataConstants.SOLUTION, solution)
        .AddRule(name, DocumentModel.DataContext.DataConstants.DOCUMENT, textControl.Document)
        .AddRule(name, TextControl.DataContext.DataConstants.TEXT_CONTROL, textControl)
        .AddRule(name, Psi.Services.DataConstants.SELECTED_EXPRESSION, expression);

      Lifetimes.Using(lifetime =>
      {
        var workflow = new IntroduceVariableWorkflow(solution, null);
        //if (nodePointer != null)
        //  workflow.Marker = nodePointer;

        if (rm != null)
        {

          var lt = Lifetimes.Define(EternalLifetime.Instance, "foo");

          workflow.EventBus = Shell.Instance.GetComponent<IEventBus>();
          workflow.EventBus
            .Event(RefactoringEvents.RefactoringFinished)
            .Subscribe(lt.Lifetime, id =>
          {
            try
            {
              textControl.Caret.MoveTo(rm.Range.EndOffset, CaretVisualPlacement.DontScrollIfVisible);
            }
            finally
            {
              lt.Terminate();
            }
          });
        }

        var dataContexts = solution.GetComponent<DataContexts>();
        WorkflowExecuter.ExecuteBatch(
          dataContexts.CreateWithDataRules(lifetime, rules), workflow);

        //MessageBox.ShowError("sdsdsd");
      });
    }
  }
}