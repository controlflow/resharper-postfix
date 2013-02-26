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
      IReferenceExpression referenceExpression, ICSharpExpression expression, IType expressionType, bool canBeStatement)
    {
      if (!canBeStatement ||
          expressionType.IsUnknown ||
          expressionType.Classify != TypeClassification.REFERENCE_TYPE) yield break;

      IDeclaredElement declaredElement = null;

      var qualifier = expression as IReferenceExpression;
      if (qualifier != null) declaredElement = qualifier.Reference.Resolve().DeclaredElement;

      var state = CSharpControlFlowNullReferenceState.UNKNOWN;

      var functionDeclaration = referenceExpression.GetContainingNode<ICSharpFunctionDeclaration>();
      if (functionDeclaration != null && declaredElement != null)
      {
        var re = ReferenceExpressionNavigator.GetByQualifierExpression(expression);
        var graf = CSharpControlFlowBuilder.Build(functionDeclaration);
        if (graf != null && re != null)
        {
          var result = graf.Inspect(ValueAnalysisMode.OPTIMISTIC);
          if (!result.HasComplexityOverflow)
          {
            foreach (var element in graf.AllElements)
            {
              if (element.SourceElement == re)
              {
                state = result.GetVariableStateAt(element, declaredElement);
                break;
              }
            }

            //ITreeNode node = expression;
            //while (node != null)
            //{
            //  
            //
            //  foreach (var element in graf.GetLeafElementsFor(node))
            //  {
            //    
            //    node = null;
            //    break;
            //  }
            //
            //  if (node != null) node = node.Parent;
            //}
          }

          
        }
      }

      if (state == CSharpControlFlowNullReferenceState.UNKNOWN ||
          state == CSharpControlFlowNullReferenceState.MAY_BE_NULL)
      {
        yield return new PostfixLookupItem("notnull", "if ($EXPR$ != null) $CARET$");
        yield return new PostfixLookupItem("null", "if ($EXPR$ == null) $CARET$");
      }
    }
  }
}