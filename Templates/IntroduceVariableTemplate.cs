using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.Application;
using JetBrains.Application.DataContext;
using JetBrains.Application.Progress;
using JetBrains.Application.Settings;
using JetBrains.DataFlow;
using JetBrains.DocumentModel;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Lookup;
using JetBrains.ReSharper.PostfixTemplates.LookupItems;
using JetBrains.ReSharper.PostfixTemplates.Settings;
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

namespace JetBrains.ReSharper.PostfixTemplates.Templates
{
  using AfterAction = Action<ITextControl, ISolution, RefactoringDetailsArgs>;

  [PostfixTemplate(
    templateName: "var",
    description: "Introduces variable for expression",
    example: "var x = expr;")]
  public sealed class IntroduceVariableTemplate : IPostfixTemplate
  {
    public ILookupItem CreateItem(PostfixTemplateContext context)
    {
      var contexts = new List<PrefixExpressionContext>();
      foreach (var expressionContext in context.ExpressionsOrTypes)
      {
        if (expressionContext.Expression is IReferenceExpression)
        {
          // filter out 'too simple' local variable expressions
          var target = expressionContext.ReferencedElement;
          if (target is IParameter || target is ILocalVariable)
          {
            if (context.IsAutoCompletion) continue;
          }
        }

        if (expressionContext.Type.IsVoid()) continue;

        var referencedType = expressionContext.ReferencedType;
        if (referencedType != null)
        {
          if (!context.IsAutoCompletion ||
            TypeUtils.IsInstantiable(referencedType, expressionContext.Expression) != 0)
          {
            contexts.Add(expressionContext);
          }

          break; // prevent from 'expr == T.var' => 'var t = expr == T;'
        }

        contexts.Add(expressionContext);
      }

      var bestContext = contexts.FirstOrDefault(ctx => ctx.CanBeStatement) ??
                        contexts.LastOrDefault(); // most outer expression
      if (bestContext == null) return null;

      if (bestContext.CanBeStatement || !context.IsAutoCompletion)
      {
        var referencedType = bestContext.ReferencedType;
        if (referencedType != null)
          return new VarByTypeItem(bestContext, referencedType);

        if (bestContext.CanBeStatement)
          return new VarStatementItem(bestContext);

        return new VarExpressionItem(bestContext);
      }

      return null;
    }

    private sealed class VarExpressionItem : ExpressionPostfixLookupItem<ICSharpExpression>
    {
      public VarExpressionItem([NotNull] PrefixExpressionContext context) : base("var", context) { }

      protected override ICSharpExpression CreateExpression(CSharpElementFactory factory,
                                                            ICSharpExpression expression)
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

    private sealed class VarStatementItem : StatementPostfixLookupItem<IExpressionStatement>
    {
      public VarStatementItem([NotNull] PrefixExpressionContext context) : base("var", context) { }

      protected override IExpressionStatement CreateStatement(CSharpElementFactory factory,
                                                              ICSharpExpression expression)
      {
        return (IExpressionStatement) factory.CreateStatement("$0;", expression);
      }

      protected override void AfterComplete(ITextControl textControl, IExpressionStatement statement)
      {
        ExecuteRefactoring(textControl, statement.Expression);
      }
    }

    private sealed class VarByTypeItem : ExpressionPostfixLookupItem<IObjectCreationExpression>
    {
      [NotNull] private readonly IDeclaredType myReferencedType;
      [NotNull] private readonly ILookupItemsOwner myLookupItemsOwner;
      private readonly bool myHasParameters;

      public VarByTypeItem([NotNull] PrefixExpressionContext context,
                           [NotNull] IDeclaredType referencedType)
        : base("var", context)
      {
        myReferencedType = referencedType;
        myLookupItemsOwner = context.PostfixContext.ExecutionContext.LookupItemsOwner;

        var instantiable = TypeUtils.IsInstantiable(referencedType, context.Expression);
        myHasParameters = (instantiable & TypeInstantiability.CtorWithParameters) != 0;
      }

      protected override IObjectCreationExpression CreateExpression(CSharpElementFactory factory,
                                                                    ICSharpExpression expression)
      {
        return (IObjectCreationExpression) factory.CreateExpression("new $0();", myReferencedType);
      }

      protected override void AfterComplete(ITextControl textControl, IObjectCreationExpression expression)
      {
        if (myHasParameters)
        {
          var lparRange = expression.LPar.GetDocumentRange();
          var rparRange = expression.RPar.GetDocumentRange();
          var rangeMarker = lparRange.SetEndTo(rparRange.TextRange.EndOffset).CreateRangeMarker();

          var settingsStore = expression.GetSettingsStore();
          var invokeParameterInfo = settingsStore.GetValue(PostfixSettingsAccessor.InvokeParameterInfo);

          ExecuteRefactoring(textControl, expression, (control, solution, _) =>
          {
            if (!rangeMarker.IsValid) return;

            var range = rangeMarker.Range;
            var offset = range.StartOffset + range.Length / 2;
            control.Caret.MoveTo(offset, CaretVisualPlacement.DontScrollIfVisible);

            if (!invokeParameterInfo) return;
            LookupUtil.ShowParameterInfo(solution, control, range, null, myLookupItemsOwner);
          });
        }
        else
        {
          ExecuteRefactoring(textControl, expression);
        }
      }
    }

    private static void ExecuteRefactoring([NotNull] ITextControl textControl,
                                           [NotNull] ICSharpExpression expression,
                                           [CanBeNull] AfterAction executeAfter = null)
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

          // todo: drop diz!
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