using System.Collections.Generic;
using JetBrains.ReSharper.ControlFlow.PostfixCompletion.LookupItems;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;

namespace JetBrains.ReSharper.ControlFlow.PostfixCompletion.TemplateProviders
{
  [PostfixTemplateProvider("var", "Introduces variable for expression")]
  public class IntroduceVariableTemplateProvider : IPostfixTemplateProvider
  {
    public IEnumerable<PostfixLookupItem> CreateItems(
      IReferenceExpression referenceExpression, ICSharpExpression expression, IType expressionType, bool canBeStatement)
    {
      if (canBeStatement)
      {
        var refExpr = expression as IReferenceExpression;
        if (refExpr != null)
        {
          var declaredElement = refExpr.Reference.Resolve().DeclaredElement;
          if (declaredElement is IParameter || declaredElement is ILocalVariable)
            yield break;
        }

        yield return new NameSuggestionPostfixLookupItem("var", "var $NAME$ = $EXPR$", expression);
      }
    }
  }
}