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
          // filter out too simple local references
          var target = exprContext.ReferencedElement;
          if (target == null || target is IParameter || target is ILocalVariable)
            continue;
        }

        if (exprContext.Type.IsVoid()) continue;

        var referencedType = exprContext.ReferencedType;
        if (referencedType != null)
        {
          if (!exprContext.CanBeStatement) continue; // can be relaxed?
          if (TypeUtils.IsInstantiable(referencedType, exprContext.Expression) == 0)
            continue;
        }

        contexts.Add(exprContext);
      }

      if (contexts.Count == 0) return;

      var bestContext = contexts.FirstOrDefault(ctx => ctx.CanBeStatement)
                     ?? contexts.FirstOrDefault();
      if (bestContext == null) return;

      if (bestContext.CanBeStatement)
      {
        var referencedType = bestContext.ReferencedType;
        if (referencedType != null)
          consumer.Add(new StatementFromTypeLookupItem(bestContext, referencedType));
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
        CSharpElementFactory factory, ICSharpExpression expression) { return expression; }

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

    private sealed class StatementFromTypeLookupItem : StatementPostfixLookupItem<IExpressionStatement>
    {
      [NotNull] private readonly IDeclaredType myReferencedType;
      private bool myHasCtorWithParams;

      public StatementFromTypeLookupItem(
        [NotNull] PrefixExpressionContext context,
        [NotNull] IDeclaredType referencedType) : base("var", context)
      {
        myReferencedType = referencedType;

        var instantiable = TypeUtils.IsInstantiable(referencedType, context.Expression);
        myHasCtorWithParams = (instantiable & TypeInstantiability.CtorWithParameters) != 0;
      }

      protected override IExpressionStatement CreateStatement(CSharpElementFactory factory)
      {
        return (IExpressionStatement) factory.CreateStatement("new $0();", myReferencedType);
      }

      protected override void PlaceExpression(
        IExpressionStatement statement, ICSharpExpression expression, CSharpElementFactory factory) { }

      protected override void AfterComplete(
        ITextControl textControl, Suffix suffix, IExpressionStatement statement, int? caretPosition)
      {
        if (myHasCtorWithParams)
        {
          var creationExpression = (IObjectCreationExpression) statement.Expression;
          var documentRange = creationExpression.LPar.GetDocumentRange();
          var rangeMarker = documentRange.EndOffsetRange().CreateRangeMarker();

          ExecuteRefactoring(textControl, statement.Expression, rangeMarker);
        }
        else
        {
          ExecuteRefactoring(textControl, statement.Expression);
        }
      }
    }

    private static void ExecuteRefactoring(
      [NotNull] ITextControl textControl, [NotNull] ICSharpExpression expression,
      [NotNull] IRangeMarker caretRangeMarker = null)
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

        // OMFG so ugly way to execute something after 'introduce variable' hotspots :(((
        if (caretRangeMarker != null)
        {
          var refactoringId = RefactoringId.FromWorkflow(workflow);
          var definition = Lifetimes.Define(EternalLifetime.Instance, name);

          workflow.EventBus = workflow.EventBus ?? Shell.Instance.GetComponent<IEventBus>();
          workflow.EventBus
            .Event(RefactoringEvents.RefactoringFinished)
            .Subscribe(definition.Lifetime, id =>
          {
            try
            {
              if (id.Equals(refactoringId) && caretRangeMarker.IsValid)
              {
                textControl.Caret.MoveTo(
                  caretRangeMarker.Range.EndOffset,
                  CaretVisualPlacement.DontScrollIfVisible);
              }
            }
            finally
            {
              definition.Terminate();
            }
          });
        }

        var dataContexts = solution.GetComponent<DataContexts>();
        WorkflowExecuter.ExecuteBatch(
          dataContexts.CreateWithDataRules(lifetime, rules), workflow);
      });
    }
  }
}