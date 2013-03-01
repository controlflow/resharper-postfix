using System.Collections.Generic;
using JetBrains.ReSharper.ControlFlow.PostfixCompletion.LookupItems;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.ControlFlow;
using JetBrains.ReSharper.Psi.ControlFlow.CSharp;

namespace JetBrains.ReSharper.ControlFlow.PostfixCompletion.TemplateProviders
{
  [PostfixTemplateProvider("null/notnull", "Checks expressions for nulls")]
  public class CheckForNullTemplateProvider : IPostfixTemplateProvider
  {
    public IEnumerable<PostfixLookupItem> CreateItems(
      ICSharpExpression expression, IType expressionType, bool canBeStatement)
    {
      if (!canBeStatement || expressionType.IsUnknown) yield break;

      var canBeNull = expressionType.IsNullable() ||
        (expressionType.Classify == TypeClassification.REFERENCE_TYPE);
      if (!canBeNull) yield break;

      IDeclaredElement declaredElement = null;

      var qualifier = expression as IReferenceExpression;
      if (qualifier != null)
        declaredElement = qualifier.Reference.Resolve().DeclaredElement;

      var state = CSharpControlFlowNullReferenceState.UNKNOWN;

      var declaration = expression.GetContainingNode<ICSharpFunctionDeclaration>();
      if (declaration != null && declaredElement != null)
      {
        var referenceExpression = ReferenceExpressionNavigator.GetByQualifierExpression(expression);
        var graph = CSharpControlFlowBuilder.Build(declaration);
        if (graph != null && referenceExpression != null)
        {
          var result = graph.Inspect(ValueAnalysisMode.OPTIMISTIC);
          if (!result.HasComplexityOverflow)
          {
            //foreach (var element in graph.GetLeafElementsFor(referenceExpression))
            foreach (var element in graph.AllElements)
            if (element.SourceElement == referenceExpression)
            {
              state = result.GetVariableStateAt(element, declaredElement); break;
            }
          }
        }
      }

      switch (state)
      {
        case CSharpControlFlowNullReferenceState.MAY_BE_NULL:
        case CSharpControlFlowNullReferenceState.UNKNOWN:
        {
          yield return new PostfixLookupItem("notnull", "if ($EXPR$ != null) $CARET$");
          yield return new PostfixLookupItem("null", "if ($EXPR$ == null) $CARET$");
          break;
        }
      }
    }
  }
}