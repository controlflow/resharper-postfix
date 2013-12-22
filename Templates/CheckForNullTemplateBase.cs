using JetBrains.Annotations;
using JetBrains.ReSharper.PostfixTemplates.LookupItems;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ControlFlow;
using JetBrains.ReSharper.Psi.ControlFlow.CSharp;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;

namespace JetBrains.ReSharper.PostfixTemplates.Templates
{
  public abstract class CheckForNullTemplateBase
  {
    protected static CSharpControlFlowNullReferenceState CheckNullabilityState(
      [NotNull] PrefixExpressionContext expressionContext)
    {
      var declaredElement = expressionContext.ReferencedElement;
      var function = expressionContext.Expression.GetContainingNode<ICSharpFunctionDeclaration>();
      if (function == null || declaredElement == null || !function.IsPhysical())
      {
        return CSharpControlFlowNullReferenceState.UNKNOWN;
      }

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

      return CSharpControlFlowNullReferenceState.UNKNOWN;
    }

    protected static bool IsNullableType([NotNull] IType type)
    {
      if (type.IsNullable()) return true;

      var classification = type.Classify;
      return classification == null || classification == TypeClassification.REFERENCE_TYPE;
    }

    protected sealed class CheckForNullItem : StatementPostfixLookupItem<IIfStatement>
    {
      [NotNull] private readonly string myTemplate;

      public CheckForNullItem([NotNull] string shortcut,
                              [NotNull] PrefixExpressionContext context,
                              [NotNull] string template)
        : base(shortcut, context)
      {
        myTemplate = template;
      }

      protected override IIfStatement CreateStatement(CSharpElementFactory factory, ICSharpExpression expression)
      {
        return (IIfStatement) factory.CreateStatement(myTemplate + EmbeddedStatementBracesTemplate, expression);
      }
    }
  }
}