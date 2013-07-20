using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.ReSharper.ControlFlow.PostfixCompletion.LookupItems;
using JetBrains.ReSharper.Feature.Services.Lookup;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ControlFlow;
using JetBrains.ReSharper.Psi.ControlFlow.CSharp;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;

namespace JetBrains.ReSharper.ControlFlow.PostfixCompletion.TemplateProviders
{
  [PostfixTemplateProvider(new[] { "null", "notnull" }, "Checks expressions for nulls")]
  public class CheckForNullTemplateProvider : IPostfixTemplateProvider
  {
    public void CreateItems(PostfixTemplateAcceptanceContext context, ICollection<ILookupItem> consumer)
    {
      var exprContext = context.PossibleExpressions.LastOrDefault();
      if (exprContext == null || !exprContext.CanBeStatement) return;

      // check expression type
      if (exprContext.Type.IsUnknown)
      {
        if (!context.ForceMode) return;
      }
      else
      {
        if (!IsNullableType(exprContext.Type)) return;
      }

      var state = CSharpControlFlowNullReferenceState.UNKNOWN;

      if (!context.ForceMode)
      {
        var declaredElement = exprContext.ReferencedElement;
        var function = exprContext.Expression.GetContainingNode<ICSharpFunctionDeclaration>();
        if (function != null && declaredElement != null && function.IsPhysical())
        {
          var graph = CSharpControlFlowBuilder.Build(function);
          if (graph != null)
          {
            var result = graph.Inspect(ValueAnalysisMode.OPTIMISTIC);
            if (!result.HasComplexityOverflow)
            {
              var referenceExpression = exprContext.Expression;

              foreach (var element in graph.AllElements)
              if (element.SourceElement == referenceExpression)
              {
                state = result.GetVariableStateAt(element, declaredElement);
                break;
              }
            }
          }
        }
      }

      switch (state)
      {
        case CSharpControlFlowNullReferenceState.MAY_BE_NULL:
        case CSharpControlFlowNullReferenceState.UNKNOWN:
          consumer.Add(new LookupItem("notnull", exprContext, "expr != null"));
          consumer.Add(new LookupItem("null", exprContext, "expr == null"));
          break;
      }
    }

    private static bool IsNullableType([NotNull] IType type)
    {
      if (type.IsNullable()) return true;

      return type.Classify == TypeClassification.REFERENCE_TYPE;
    }

    private sealed class LookupItem : KeywordStatementPostfixLookupItemBase
    {
      [NotNull] private readonly string myCondition;

      protected override string Keyword { get { return "if"; } }

      public LookupItem([NotNull] string shortcut,
        [NotNull] PrefixExpressionContext context, [NotNull] string condition)
        : base(shortcut, context)
      {
        myCondition = condition;
      }

      protected override void PlaceExpression(
        IIfStatement statement, ICSharpExpression expression, CSharpElementFactory factory)
      {
        var newCheckExpr = (IEqualityExpression) factory.CreateExpression(myCondition);
        var checkedExpr = statement.Condition.ReplaceBy(newCheckExpr);
        checkedExpr.LeftOperand.ReplaceBy(expression);
      }
    }
  }
}