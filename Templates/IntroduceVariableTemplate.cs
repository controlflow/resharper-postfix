using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.Application;
using JetBrains.Application.DataContext;
using JetBrains.Application.Progress;
using JetBrains.DataFlow;
using JetBrains.DocumentModel;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.ControlFlow.PostfixCompletion.LookupItems;
using JetBrains.ReSharper.Feature.Services.Lookup;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Search;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Refactorings.IntroduceVariable;
using JetBrains.ReSharper.Refactorings.WorkflowNew;
using JetBrains.TextControl;
using JetBrains.Util.EventBus;

namespace JetBrains.ReSharper.ControlFlow.PostfixCompletion.Templates
{
  [PostfixTemplate(
    templateName: "var",
    description: "Introduces variable for expression",
    example: "var x = expr;", WorksOnTypes = true)]
  public sealed class IntroduceVariableTemplate : IPostfixTemplate
  {
    public ILookupItem CreateItems(PostfixTemplateContext context)
    {
      var contexts = new List<PrefixExpressionContext>();
      foreach (var exprContext in context.Expressions)
      {
        if (exprContext.Expression is IReferenceExpression)
        {
          // filter out too simple local references
          var target = exprContext.ReferencedElement;
          if (target is IParameter || target is ILocalVariable) continue;

          // filter out namespaces
          if (target is INamespace) continue;
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

      if (contexts.Count == 0) return null;

      var bestContext = contexts.FirstOrDefault(ctx => ctx.CanBeStatement) ??
                        contexts.FirstOrDefault();
      if (bestContext == null) return null;

      if (bestContext.CanBeStatement)
      {
        var referencedType = bestContext.ReferencedType;
        if (referencedType == null)
          return new IntroduceVarStatementItem(bestContext);

        var lookupItemsOwner = context.ExecutionContext.LookupItemsOwner;
        return new IntroduceVarByTypeItem(bestContext, referencedType, lookupItemsOwner);
      }

      if (context.IsForceMode)
      {
        return new IntroduceVarExpressionItem(bestContext);
      }

      return null;
    }

    private sealed class IntroduceVarExpressionItem : ExpressionPostfixLookupItem<ICSharpExpression>
    {
      public IntroduceVarExpressionItem([NotNull] PrefixExpressionContext context) : base("var", context) { }

      protected override ICSharpExpression CreateExpression(
        CSharpElementFactory factory, ICSharpExpression expression)
      {
        return expression;
      }

      protected override void AfterComplete(ITextControl textControl, ICSharpExpression expression)
      {
        ExecuteRefactoring(textControl, expression,
          (control, solution, args) =>
          {
            var localVariable = args.Properties["result"] as ILocalVariable;
            if (localVariable == null) return;

            var declaration = localVariable.GetDeclarations().FirstOrDefault();
            if (declaration == null) return;

            var scope = declaration.GetContainingNode<IBlock>();
            if (scope == null) return;

            var factory = solution.GetComponent<SearchDomainFactory>();
            var searchDomain = factory.CreateSearchDomain(scope);
            var references = solution.GetPsiServices().Finder.FindReferences(
              localVariable, searchDomain, NullProgressIndicator.Instance);

            if (references.Length == 1)
            {
              var endOffset = references[0].GetDocumentRange().TextRange.EndOffset;
              textControl.Caret.MoveTo(endOffset, CaretVisualPlacement.DontScrollIfVisible);
            }
          });
      }
    }

    private sealed class IntroduceVarStatementItem : StatementPostfixLookupItem<IExpressionStatement>
    {
      public IntroduceVarStatementItem([NotNull] PrefixExpressionContext context) : base("var", context) { }

      protected override IExpressionStatement CreateStatement(
        CSharpElementFactory factory, ICSharpExpression expression)
      {
        return (IExpressionStatement) factory.CreateStatement("$0;", expression);
      }

      protected override void AfterComplete(ITextControl textControl, IExpressionStatement statement)
      {
        ExecuteRefactoring(textControl, statement.Expression);
      }
    }

    private sealed class IntroduceVarByTypeItem : StatementPostfixLookupItem<IExpressionStatement>
    {
      [NotNull] private readonly IDeclaredType myReferencedType;
      [NotNull] private readonly ILookupItemsOwner myLookupItemsOwner;
      private readonly bool myHasCtorWithParams;

      public IntroduceVarByTypeItem(
        [NotNull] PrefixExpressionContext context, [NotNull] IDeclaredType referencedType,
        [NotNull] ILookupItemsOwner lookupItemsOwner) : base("var", context)
      {
        myReferencedType = referencedType;
        myLookupItemsOwner = lookupItemsOwner;

        var instantiable = TypeUtils.IsInstantiable(referencedType, context.Expression);
        myHasCtorWithParams = (instantiable & TypeInstantiability.CtorWithParameters) != 0;
      }

      protected override IExpressionStatement CreateStatement(
        CSharpElementFactory factory, ICSharpExpression expression)
      {
        return (IExpressionStatement) factory.CreateStatement("new $0();", myReferencedType);
      }

      protected override void AfterComplete(ITextControl textControl, IExpressionStatement statement)
      {
        if (myHasCtorWithParams)
        {
          var creationExpression = (IObjectCreationExpression) statement.Expression;
          var lparRange = creationExpression.LPar.GetDocumentRange();
          var rparRange = creationExpression.RPar.GetDocumentRange();
          var rangeMarker = lparRange.SetEndTo(rparRange.TextRange.EndOffset).CreateRangeMarker();

          ExecuteRefactoring(textControl, statement.Expression, (control, solution, _) =>
          {
            if (rangeMarker.IsValid)
            {
              var range = rangeMarker.Range;
              var offset = range.StartOffset + range.Length/2;
              control.Caret.MoveTo(offset, CaretVisualPlacement.DontScrollIfVisible);

              LookupUtil.ShowParameterInfo(
                solution, control, range, null, myLookupItemsOwner);
            }
          });
        }
        else
        {
          ExecuteRefactoring(textControl, statement.Expression);
        }
      }
    }

    private static void ExecuteRefactoring(
      [NotNull] ITextControl textControl, [NotNull] ICSharpExpression expression,
      [CanBeNull] Action<ITextControl, ISolution, RefactoringDetailsArgs> executeAfter = null)
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
        if (executeAfter != null)
        {
          var refactoringId = RefactoringId.FromWorkflow(workflow);
          var definition = Lifetimes.Define(EternalLifetime.Instance, name);

          workflow.EventBus = workflow.EventBus ?? Shell.Instance.GetComponent<IEventBus>();

          RefactoringDetailsArgs details = null;

          workflow.EventBus
            .Event(RefactoringEvents.RefactoringDetails)
            .Subscribe(definition.Lifetime, args => details = args);

          workflow.EventBus
            .Event(RefactoringEvents.RefactoringFinished)
            .Subscribe(definition.Lifetime, id =>
            {
              try
              {
                if (id.Equals(refactoringId))
                  executeAfter(textControl, solution, details);
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