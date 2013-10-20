using JetBrains.Annotations;
using JetBrains.ReSharper.ControlFlow.PostfixCompletion.LookupItems;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ControlFlow;
using JetBrains.ReSharper.Psi.ControlFlow.CSharp;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;

namespace JetBrains.ReSharper.ControlFlow.PostfixCompletion.TemplateProviders
{
  public abstract class CheckForNullTemplateProviderBase
  {
    protected static CSharpControlFlowNullReferenceState CheckNullabilityState(
      [NotNull] PrefixExpressionContext expressionContext)
    {
      var declaredElement = expressionContext.ReferencedElement;
      var function = expressionContext.Expression.GetContainingNode<ICSharpFunctionDeclaration>();
      if (function != null && declaredElement != null && function.IsPhysical())
      {
        var graph = CSharpControlFlowBuilder.Build(function);
        if (graph != null)
        {
          var result = graph.Inspect(ValueAnalysisMode.OPTIMISTIC);
          if (!result.HasComplexityOverflow)
          {
            var referenceExpression = expressionContext.Expression;

            foreach (var element in graph.AllElements)
              if (element.SourceElement == referenceExpression)
                return result.GetVariableStateAt(element, declaredElement);
          }
        }
      }

      return CSharpControlFlowNullReferenceState.UNKNOWN;
    }

    protected static bool IsNullableType([NotNull] IType type)
    {
      if (type.IsNullable()) return true;

      var classification = type.Classify;
      return classification == null
             || classification == TypeClassification.REFERENCE_TYPE;
    }

    protected sealed class LookupItem : KeywordStatementPostfixLookupItem<IIfStatement>
    {
      [NotNull]
      private readonly string myTemplate;

      public LookupItem([NotNull] string shortcut,
        [NotNull] PrefixExpressionContext context, [NotNull] string template)
        : base(shortcut, context)
      {
        myTemplate = template;
      }

      protected override string Template { get { return myTemplate; } }

      protected override void PlaceExpression(
        IIfStatement statement, ICSharpExpression expression, CSharpElementFactory factory)
      {
        var checkedExpr = (IEqualityExpression)statement.Condition;
        checkedExpr.LeftOperand.ReplaceBy(expression);
      }
    }
  }
}