using System.Collections.Generic;
using JetBrains.ReSharper.ControlFlow.PostfixCompletion.LookupItems;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Impl;

namespace JetBrains.ReSharper.ControlFlow.PostfixCompletion.TemplateProviders
{
  [PostfixTemplateProvider("throw", "Throw expression of 'Exception' type")]
  public class ThrowStatementTemplateProvider : IPostfixTemplateProvider
  {
    public IEnumerable<PostfixLookupItem> CreateItems(PostfixTemplateAcceptanceContext context)
    {
      if (context.CanBeStatement)
      {
        if (!context.LooseChecks)
        {
          if (context.ExpressionType.IsUnknown) yield break;

          var rule = context.Expression.GetTypeConversionRule();
          var predefinedType = context.Expression.GetPsiModule().GetPredefinedType();
          if (!rule.IsImplicitlyConvertibleTo(context.ExpressionType, predefinedType.Exception))
            yield break;
        }

        yield return new PostfixLookupItem("throw", "throw $EXPR$");
      }
    }
  }
}