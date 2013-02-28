using System.Collections.Generic;
using JetBrains.ReSharper.ControlFlow.PostfixCompletion.LookupItems;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.CSharp.Impl;

namespace JetBrains.ReSharper.ControlFlow.PostfixCompletion.TemplateProviders
{
  [PostfixTemplateProvider("return", "Returns expression")]
  public class ReturnStatementTemplateProvider : IPostfixTemplateProvider
  {
    public IEnumerable<PostfixLookupItem> CreateItems(
      ICSharpExpression expression, IType expressionType, bool canBeStatement)
    {
      if (!canBeStatement || expressionType.IsUnknown) yield break;

      var declaration = expression.GetContainingNode<ICSharpFunctionDeclaration>();
      if (declaration != null && !declaration.IsAsync && !declaration.IsIterator)
      {
        var declaredElement = declaration.DeclaredElement;
        if (declaredElement != null)
        {
          var returnType = declaredElement.ReturnType;
          if (returnType.IsVoid()) yield break;

          var rule = expression.GetTypeConversionRule();
          if (rule.IsImplicitlyConvertibleTo(expressionType, returnType))
          {
            yield return new PostfixLookupItem("return", "return $EXPR$");
          }
        }
      }
    }
  }
}