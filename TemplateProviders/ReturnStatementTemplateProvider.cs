using System.Collections.Generic;
using JetBrains.ReSharper.ControlFlow.PostfixCompletion.LookupItems;
using JetBrains.ReSharper.Feature.Services.Lookup;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Impl;

namespace JetBrains.ReSharper.ControlFlow.PostfixCompletion.TemplateProviders
{
  [PostfixTemplateProvider("return", "Returns expression")]
  public class ReturnStatementTemplateProvider : IPostfixTemplateProvider
  {
    public void CreateItems(PostfixTemplateAcceptanceContext context, ICollection<ILookupItem> consumer)
    {
      if (!context.CanBeStatement) return;
      if (!context.LooseChecks && context.ExpressionType.IsUnknown) return;

      var declaration = context.ContainingFunction;
      if (declaration != null && !declaration.IsAsync && !declaration.IsIterator)
      {
        var declaredElement = declaration.DeclaredElement;
        if (declaredElement != null)
        {
          var returnType = declaredElement.ReturnType;
          if (returnType.IsVoid()) return;

          if (!context.LooseChecks)
          {
            var rule = context.Expression.GetTypeConversionRule();
            if (!rule.IsImplicitlyConvertibleTo(context.ExpressionType, returnType)) return;
          }

          consumer.Add(new PostfixLookupItem(context, "return", "return $EXPR$"));
        }
      }
    }
  }
}