using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using JetBrains.ActionManagement;
using JetBrains.Annotations;
using JetBrains.Application.DataContext;
using JetBrains.Application.Settings;
using JetBrains.DataFlow;
using JetBrains.DocumentModel;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.Hotspots;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.LiveTemplates;
using JetBrains.ReSharper.Feature.Services.Lookup;
using JetBrains.ReSharper.Feature.Services.Refactorings;
using JetBrains.ReSharper.Feature.Services.Util;
using JetBrains.ReSharper.PostfixTemplates.CodeCompletion;
using JetBrains.ReSharper.PostfixTemplates.Contexts.CSharp;
using JetBrains.ReSharper.PostfixTemplates.LookupItems;
using JetBrains.ReSharper.PostfixTemplates.Settings;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Refactorings.IntroduceVariable;
using JetBrains.ReSharper.Refactorings.IntroduceVariable.Impl;
using JetBrains.ReSharper.Resources.Shell;
using JetBrains.TextControl;
using JetBrains.Util;
using JetBrains.Util.EventBus;

// todo: think about cases like F(this.var), F(42.var). disable in auto?

namespace JetBrains.ReSharper.PostfixTemplates.Templates.CSharp
{
  [PostfixTemplate(
    templateName: "var",
    description: "Introduces variable for expression",
    example: "var x = expr;")]
  public sealed class IntroduceVariableTemplate : IPostfixTemplate<CSharpPostfixTemplateContext>
  {
    [SuppressMessage("ReSharper", "PossibleInvalidCastExceptionInForeachLoop")]
    public PostfixTemplateInfo TryCreateInfo(CSharpPostfixTemplateContext context)
    {
      var contexts = new List<CSharpPostfixExpressionContext>();

      foreach (CSharpPostfixExpressionContext expressionContext in context.AllExpressions)
      {
        var expression = expressionContext.Expression;
        if (expression is IReferenceExpression)
        {
          // filter out 'too simple' local variable expressions
          var target = expressionContext.ReferencedElement;
          if (target is IParameter || target is ILocalVariable)
          {
            if (context.IsPreciseMode) continue;
          }
        }
        else if (expression is IAssignmentExpression)
        {
          if (context.IsPreciseMode) continue;
        }

        if (expressionContext.Type.IsVoid()) continue;

        var referencedType = expressionContext.ReferencedType;
        if (referencedType != null)
        {
          if (context.IsPreciseMode)
          {
            if (TypeUtils.CanInstantiateType(referencedType, expression) == 0) break;
            if (!TypeUtils.IsUsefulToCreateWithNew(referencedType.GetTypeElement())) break;
          }

          contexts.Add(expressionContext);
          break; // prevent from 'expr == T.var' => 'var t = expr == T;'
        }

        contexts.Add(expressionContext);
      }

      var bestContext = contexts.FirstOrDefault(IsItMattersToShowVar) ?? contexts.LastOrDefault();
      if (bestContext != null)
      {
        if (IsItMattersToShowVar(bestContext) || !context.IsPreciseMode)
        {
          var referencedType = bestContext.ReferencedType;
          if (referencedType != null)
            return new PostfixTemplateInfo("var", bestContext, target: PostfixTemplateTarget.TypeUsage);

          if (bestContext.CanBeStatement)
            return new PostfixTemplateInfo("var", bestContext, target: PostfixTemplateTarget.Statement);

          return new PostfixTemplateInfo("var", bestContext);
        }
      }

      return null;
    }

    private static bool IsConstructorInvocation([NotNull] ICSharpExpression expression)
    {
      // check for expressions like 'StringBuilder().new'
      var invocationExpression = expression as IInvocationExpression;
      if (invocationExpression == null) return false;

      var reference = invocationExpression.InvokedExpression as IReferenceExpression;
      if (reference != null && CSharpPostfixUtis.IsReferenceExpressionsChain(reference))
      {
        var resolveResult = reference.Reference.Resolve().Result;
        return resolveResult.DeclaredElement is ITypeElement;
      }

      return false;
    }

    private static bool IsItMattersToShowVar([NotNull] CSharpPostfixExpressionContext context)
    {
      if (context.CanBeStatement) return true;

      var withReference = context.ExpressionWithReference;
      if (withReference != null)
      {
        // return SomeLong().var.Expression;
        var outerReference = ReferenceExpressionNavigator.GetByQualifierExpression(withReference);
        if (outerReference != null) return true;

        // SomeCall(withComplex.Arguments().var, 42);
        var argument = CSharpArgumentNavigator.GetByValue(withReference);
        if (argument != null) return true;
      }

      // note: what about F(arg.var)?
      return false;
    }

    public PostfixTemplateBehavior CreateBehavior(PostfixTemplateInfo info)
    {
      switch (info.Target)
      {
        case PostfixTemplateTarget.TypeUsage:
          return new CSharpPostfixIntroduceVariableFromTypeUsageBehavior(info);

        case PostfixTemplateTarget.Statement:
          return new CSharpPostfixIntroduceVariableIsStatementBehavior(info);

        case PostfixTemplateTarget.Expression:
          return new CSharpPostfixIntroduceVariableInExpressionBehavior(info);

        default:
          throw new ArgumentOutOfRangeException();
      }
    }

    private sealed class CSharpPostfixIntroduceVariableInExpressionBehavior : CSharpExpressionPostfixTemplateBehavior<ICSharpExpression>
    {
      public CSharpPostfixIntroduceVariableInExpressionBehavior([NotNull] PostfixTemplateInfo info) : base(info) { }

      protected override ICSharpExpression CreateExpression(CSharpElementFactory factory, ICSharpExpression expression)
      {
        if (IsConstructorInvocation(expression))
        {
          // note: reinterpret constructor call as object creation expression
          return factory.CreateExpression("new $0", expression.GetText());
        }

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

          var reference = TextControlToPsi.GetElement<IReferenceExpression>(solution, textControl.Document, referenceRange.EndOffset);

          if (reference != null && reference.QualifierExpression == null)
          {
            var endOffset = reference.GetDocumentRange().TextRange.EndOffset;
            textControl.Caret.MoveTo(endOffset, CaretVisualPlacement.DontScrollIfVisible);
          }
        });
      }
    }

    private sealed class CSharpPostfixIntroduceVariableIsStatementBehavior : CSharpStatementPostfixTemplateBehavior<IExpressionStatement>
    {
      public CSharpPostfixIntroduceVariableIsStatementBehavior([NotNull] PostfixTemplateInfo info) : base(info) { }

      protected override IExpressionStatement CreateStatement(CSharpElementFactory factory, ICSharpExpression expression)
      {
        if (IsConstructorInvocation(expression))
        {
          // note: reinterpret constructor call as object creation expression
          expression = factory.CreateExpression("new $0", expression.GetText());
        }

        return (IExpressionStatement) factory.CreateStatement("$0;", expression);
      }

      protected override void AfterComplete(ITextControl textControl, IExpressionStatement statement)
      {
        ExecuteRefactoring(textControl, statement.Expression);
      }
    }

    private sealed class CSharpPostfixIntroduceVariableFromTypeUsageBehavior : CSharpExpressionPostfixTemplateBehavior<IObjectCreationExpression>
    {
      public CSharpPostfixIntroduceVariableFromTypeUsageBehavior([NotNull] PostfixTemplateInfo info) : base(info) { }

      protected override IObjectCreationExpression CreateExpression(CSharpElementFactory factory, ICSharpExpression expression)
      {
        // todo: not sure it will always work, let's make it safier
        var referenceExpression = (IReferenceExpression) expression.NotNull("referenceExpression != null");
        var resolveResult = referenceExpression.Reference.Resolve().Result;

        var typeElement = (ITypeElement) resolveResult.DeclaredElement.NotNull("typeElement != null");
        var referencedType = TypeFactory.CreateType(typeElement, resolveResult.Substitution);

        return (IObjectCreationExpression) factory.CreateExpression("new $0()", referencedType);
      }

      protected override void AfterComplete(ITextControl textControl, IObjectCreationExpression expression)
      {
        var referencedType = CSharpTypeFactory.CreateDeclaredType(expression.CreatedTypeUsage);

        var canInstantiate = TypeUtils.CanInstantiateType(referencedType, expression);
        if ((canInstantiate & CanInstantiate.ConstructorWithParameters) != 0)
        {
          var lparRange = expression.LPar.GetDocumentRange();
          var rparRange = expression.RPar.GetDocumentRange();

          var documentRange = lparRange.SetEndTo(rparRange.TextRange.EndOffset);
          var argumentsMarker = documentRange.CreateRangeMarker();

          var settingsStore = expression.GetSettingsStore();
          var invokeParameterInfo = settingsStore.GetValue(PostfixTemplatesSettingsAccessor.InvokeParameterInfo);

          var solution = expression.GetSolution();

          ExecuteRefactoring(textControl, expression, executeAfter: () =>
          {
            var argumentsRange = argumentsMarker.Range;
            if (!argumentsRange.IsValid) return;

            var offset = argumentsRange.StartOffset + argumentsRange.Length/2; // EWW
            textControl.Caret.MoveTo(offset, CaretVisualPlacement.DontScrollIfVisible);

            if (invokeParameterInfo)
            {
              var lookupItemsOwner = Info.ExecutionContext.LookupItemsOwner;
              LookupUtil.ShowParameterInfo(solution, textControl, lookupItemsOwner);
            }
          });
        }
        else
        {
          ExecuteRefactoring(textControl, expression);
        }
      }
    }

    private static void ExecuteRefactoring([NotNull] ITextControl textControl, [NotNull] ICSharpExpression expression, [CanBeNull] Action executeAfter = null)
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
      var multipleOccurrences = settingsStore.GetValue(PostfixTemplatesSettingsAccessor.SearchVarOccurrences);

      // note: uber ugly code down here
      using (var definition = Lifetimes.Define(EternalLifetime.Instance, actionId))
      {
        var dataContexts = solution.GetComponent<DataContexts>();
        var dataContext = dataContexts.CreateWithDataRules(definition.Lifetime, rules);

        // todo: introduce normal way to execute refactorings with occurences search
        if (multipleOccurrences && !Shell.Instance.IsTestShell)
        {
          var introduceAction = new IntroVariableAction();
          if (introduceAction.Update(dataContext, new ActionPresentation(), () => false))
          {
            introduceAction.Execute(dataContext, delegate { });

            if (executeAfter != null) IntroduceVariableTemplate.SubscribeAfterExecute(executeAfter);
          }
        }
        else
        {
          var workflow = new IntroduceVariableWorkflow(solution, actionId);
          WorkflowExecuter.ExecuteBatch(dataContext, workflow);

          var finishedAction = executeAfter;
          if (finishedAction != null)
          {
            var currentSession = HotspotSessionExecutor.Instance.CurrentSession;
            if (currentSession != null) // ugly hack
            {
              currentSession.HotspotSession.Closed.Advise(EternalLifetime.Definition, (e) =>
              {
                if (e.TerminationType == TerminationType.Finished)
                {
                  finishedAction();
                }
              });
            }
            else
            {
              finishedAction();
            }
          }
        }
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
          {
            action();
          }
        }
        finally
        {
          definition.Terminate();
        }
      });
    }
  }
}
