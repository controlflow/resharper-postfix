using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.Application;
using JetBrains.Application.DataContext;
using JetBrains.DataFlow;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.ControlFlow.PostfixCompletion.LookupItems;
using JetBrains.ReSharper.Feature.Services.Lookup;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Refactorings.IntroduceVariable;
using JetBrains.ReSharper.Refactorings.WorkflowNew;
using JetBrains.TextControl;
using JetBrains.TextControl.Coords;
using JetBrains.Util;
using DataConstants = JetBrains.DocumentModel.DataContext.DataConstants;

namespace JetBrains.ReSharper.ControlFlow.PostfixCompletion.TemplateProviders
{
  [PostfixTemplateProvider("var", "Introduces variable for expression")]
  public class IntroduceVariableTemplateProvider : IPostfixTemplateProvider
  {
    public void CreateItems(PostfixTemplateAcceptanceContext context, ICollection<ILookupItem> consumer)
    {
      if (context.Expression is IReferenceExpression)
      {
        // filter out too simple locals expressions
        var target = context.ExpressionReferencedElement;
        if (target == null || target is IParameter || target is ILocalVariable)
          return;
      }

      if (context.CanBeStatement)
      {
        consumer.Add(new NameSuggestionPostfixLookupItem(
          context, "var", "var $NAME$ = $EXPR$", context.Expression));
      }
      else if (context.LooseChecks)
      {
        consumer.Add(new IntroduceVariableLookupItem(context));
      }
    }

    private class IntroduceVariableLookupItem : ProcessExpressionPostfixLookupItem
    {
      public IntroduceVariableLookupItem([NotNull] PostfixTemplateAcceptanceContext context)
        : base(context, "var") { }

      protected override void AcceptExpression(
        ITextControl textControl, ISolution solution, TextRange resultRange, ICSharpExpression expression)
      {
        // set selection for introduce viriable
        textControl.Selection.SetRanges(new[] { TextControlPosRange.FromDocRange(textControl, resultRange) });

        const string name = "IntroVariableAction";
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