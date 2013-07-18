using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.Application;
using JetBrains.Application.DataContext;
using JetBrains.DataFlow;
using JetBrains.ProjectModel;
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
        var target = context.ReferencedElement;
        if (target == null || target is IParameter || target is ILocalVariable)
          return;
      }

      if (context.CanBeStatement || context.LooseChecks)
      {
        consumer.Add(new IntroduceVariableLookupItem(context.PossibleExpressions.First()));
      }
    }

    private class IntroduceVariableLookupItem : PostfixStatementLookupItem<IExpressionStatement>
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
        ITextControl textControl, Suffix suffix, IExpressionStatement expressionStatement, int? caretPosition)
      {
        // todo: suffix?

        if (expressionStatement != null)
        {
          // set selection for introduce viriable
          // todo: do we need this?
          textControl.Selection.SetRanges(new[] {
            TextControlPosRange.FromDocRange(
              textControl, expressionStatement.Expression.GetDocumentRange().TextRange)
          });

          const string name = "IntroVariableAction";
          var solution = expressionStatement.GetSolution();
          var rules = DataRules
            .AddRule(name, ProjectModel.DataContext.DataConstants.SOLUTION, solution)
            .AddRule(name, DataConstants.DOCUMENT, textControl.Document)
            .AddRule(name, TextControl.DataContext.DataConstants.TEXT_CONTROL, textControl)
            .AddRule(name, Psi.Services.DataConstants.SELECTED_EXPRESSION, expressionStatement.Expression);

          Lifetimes.Using(lifetime =>
            WorkflowExecuter.ExecuteBatch(
              Shell.Instance.GetComponent<DataContexts>().CreateWithDataRules(lifetime, rules),
              new IntroduceVariableWorkflow(solution, null)));
        }

        
      }
    }
  }
}