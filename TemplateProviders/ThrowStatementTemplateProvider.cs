using System.Collections.Generic;
using JetBrains.ReSharper.ControlFlow.PostfixCompletion.LookupItems;
using JetBrains.ReSharper.Feature.Services.Lookup;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Impl;

namespace JetBrains.ReSharper.ControlFlow.PostfixCompletion.TemplateProviders
{
  [PostfixTemplateProvider("throw", "Throw expression of 'Exception' type")]
  public class ThrowStatementTemplateProvider : IPostfixTemplateProvider
  {
    public void CreateItems(PostfixTemplateAcceptanceContext context, ICollection<ILookupItem> consumer)
    {
      if (context.CanBeStatement)
      {
        if (!context.LooseChecks)
        {
          if (context.ExpressionType.IsUnknown) return;

          var rule = context.Expression.GetTypeConversionRule();
          var predefinedType = context.Expression.GetPredefinedType();
          if (!rule.IsImplicitlyConvertibleTo(context.ExpressionType, predefinedType.Exception))
            return;
        }

        consumer.Add(new PostfixLookupItemObsolete(context, "throw", "throw $EXPR$"));
      }
    }
  }
}