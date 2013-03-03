using System.Collections.Generic;
using JetBrains.ReSharper.ControlFlow.PostfixCompletion.LookupItems;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Impl;

namespace JetBrains.ReSharper.ControlFlow.PostfixCompletion.TemplateProviders
{
  [PostfixTemplateProvider("return", "Returns expression")]
  public class ReturnStatementTemplateProvider : IPostfixTemplateProvider
  {
    public IEnumerable<PostfixLookupItem> CreateItems(PostfixTemplateAcceptanceContext context)
    {
      if (!context.CanBeStatement) yield break;
      if (!context.LooseChecks && context.ExpressionType.IsUnknown)
        yield break;

      var declaration = context.ContainingFunction;
      if (declaration != null && !declaration.IsAsync && !declaration.IsIterator)
      {
        var declaredElement = declaration.DeclaredElement;
        if (declaredElement != null)
        {
          var returnType = declaredElement.ReturnType;
          if (returnType.IsVoid())
            yield break;

          if (!context.LooseChecks)
          {
            var rule = context.Expression.GetTypeConversionRule();
            if (!rule.IsImplicitlyConvertibleTo(context.ExpressionType, returnType))
              yield break;
          }

          yield return new PostfixLookupItem("return", "return $EXPR$");
        }
      }
    }
  }
}