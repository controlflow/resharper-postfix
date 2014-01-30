using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.ActionManagement;
using JetBrains.Annotations;
using JetBrains.Application;
using JetBrains.Application.DataContext;
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
using JetBrains.ReSharper.Psi.Services;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Refactorings.IntroduceVariable;
using JetBrains.ReSharper.Refactorings.IntroduceVariable.Impl;
using JetBrains.ReSharper.Refactorings.WorkflowNew;
using JetBrains.TextControl;
using JetBrains.Util.EventBus;

namespace JetBrains.ReSharper.PostfixTemplates.Templates
{
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
          if (context.IsAutoCompletion)
          {
            if (TypeUtils.CanInstantiateType(referencedType, expressionContext.Expression) == 0) break;
            if (!TypeUtils.IsUsefulToCreateWithNew(referencedType.GetTypeElement())) break;
          }

          contexts.Add(expressionContext);
          break; // prevent from 'expr == T.var' => 'var t = expr == T;'
        }

        contexts.Add(expressionContext);
      }

      var bestContext = contexts.FirstOrDefault(c => c.CanBeStatement) ??
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
        var expressionRange = expression.GetDocumentRange();
        var expressionMarker = expressionRange.CreateRangeMarker();
        var solution = expression.GetSolution();

        ExecuteRefactoring(textControl, expression, () =>
        {
          var referenceRange = expressionMarker.Range;
          if (!referenceRange.IsValid) return;

          var reference = TextControlToPsi.GetElement<IReferenceExpression>(
            solution, textControl.Document, referenceRange.EndOffset);

          if (reference != null && reference.QualifierExpression == null)
          {
            var endOffset = reference.GetDocumentRange().TextRange.EndOffset;
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

        var canInstantiate = TypeUtils.CanInstantiateType(referencedType, context.Expression);
        myHasParameters = (canInstantiate & CanInstantiate.ConstructorWithParameters) != 0;
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

          var documentRange = lparRange.SetEndTo(rparRange.TextRange.EndOffset);
          var argumentsMarker = documentRange.CreateRangeMarker();

          var settingsStore = expression.GetSettingsStore();
          var invokeParameterInfo = settingsStore.GetValue(PostfixSettingsAccessor.InvokeParameterInfo);
          var solution = expression.GetSolution();

          ExecuteRefactoring(textControl, expression, () =>
          {
            var argumentsRange = argumentsMarker.Range;
            if (!argumentsRange.IsValid) return;

            var offset = argumentsRange.StartOffset + argumentsRange.Length / 2;
            textControl.Caret.MoveTo(offset, CaretVisualPlacement.DontScrollIfVisible);

            if (invokeParameterInfo)
            {
              LookupUtil.ShowParameterInfo(
                solution, textControl, argumentsRange, null, myLookupItemsOwner);
            }
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
                                           [CanBeNull] Action executeAfter = null)
    {
      const string actionId = IntroVariableAction.ACTION_ID;

      var solution = expression.GetSolution();
      var document = textControl.Document;

      var expressionRange = expression.GetDocumentRange().TextRange;
      textControl.Selection.SetRange(expressionRange);

      var rules = DataRules
        .AddRule(actionId, ProjectModel.DataContext.DataConstants.SOLUTION, solution)
        .AddRule(actionId, DocumentModel.DataContext.DataConstants.DOCUMENT, document)
        .AddRule(actionId, TextControl.DataContext.DataConstants.TEXT_CONTROL, textControl);

      var settingsStore = expression.GetSettingsStore();
      var multipleOccurrences = settingsStore.GetValue(PostfixSettingsAccessor.SearchVarOccurrences);

      var definition = Lifetimes.Define(EternalLifetime.Instance, actionId);
      try // note: uber ugly code down here
      {
        var dataContexts = solution.GetComponent<DataContexts>();
        var dataContext = dataContexts.CreateWithDataRules(definition.Lifetime, rules);

        #pragma warning disable 618
        if (multipleOccurrences && !Shell.Instance.IsTestShell)
        #pragma warning restore 618
        {
          var introduceAction = new IntroVariableAction();
          if (introduceAction.Update(dataContext, new ActionPresentation(), () => false))
          {
            introduceAction.Execute(dataContext, delegate { });
          }
        }
        else
        {
          var workflow = new IntroduceVariableWorkflow(solution, actionId);
          WorkflowExecuter.ExecuteBatch(dataContext, workflow);
        }

        if (executeAfter != null) SubscribeAfterExecute(executeAfter);
      }
      finally
      {
        definition.Terminate();
      }
    }

    private static void SubscribeAfterExecute([NotNull] Action action)
    {
      // the only way to listen refactoring finish event is IEventBus
      var eventBus = Shell.Instance.GetComponent<IEventBus>();
      var finished = eventBus.Event(RefactoringEvents.RefactoringFinished);
      var definition = Lifetimes.Define(EternalLifetime.Instance, IntroVariableAction.ACTION_ID);

      finished.Subscribe(definition.Lifetime, id =>
      {
        try
        {
          if (id.Title == "Introduce Variable") // :((
            action();
        }
        finally
        {
          definition.Terminate();
        }
      });
    }
  }
}