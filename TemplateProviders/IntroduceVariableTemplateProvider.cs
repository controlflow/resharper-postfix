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
#if RESHARPER8

#endif

namespace JetBrains.ReSharper.ControlFlow.PostfixCompletion.TemplateProviders
{
  // todo: support for occurances?

  [PostfixTemplateProvider("var", "Introduces variable for expression")]
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
      var bestContext = contexts.FirstOrDefault(ctx => ctx.CanBeStatement) ?? contexts.FirstOrDefault();
      if (bestContext == null) return;

      if (bestContext.CanBeStatement || context.ForceMode)
        consumer.Add(new LookupItem(bestContext));
    }

    private sealed class LookupItem : ExpressionPostfixLookupItem<ICSharpExpression>
    {
      public LookupItem([NotNull] PrefixExpressionContext context)
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

        if (expression == null) return;

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
}