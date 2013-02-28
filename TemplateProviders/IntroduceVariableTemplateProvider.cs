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
      ICSharpExpression expression, IType expressionType, bool canBeStatement)
    {
      // todo: relax this restriction
      if (!canBeStatement) yield break;

      // filter out too simple locals expressions
      var referenceExpression = expression as IReferenceExpression;
      if (referenceExpression != null)
      {
        var declaredElement = referenceExpression.Reference.Resolve().DeclaredElement;
        if (declaredElement is IParameter || declaredElement is ILocalVariable)
          yield break;
      }

      yield return new NameSuggestionPostfixLookupItem("var", "var $NAME$ = $EXPR$", expression);
    }
  }
}