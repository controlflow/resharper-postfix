using System.Collections.Generic;
using JetBrains.ReSharper.ControlFlow.PostfixCompletion.LookupItems;
using JetBrains.ReSharper.Feature.Services.Lookup;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.ControlFlow.PostfixCompletion.TemplateProviders
{
  [PostfixTemplateProvider("ifnot", "Checks boolean expression to be 'false'")]
  public class IfNotStatementTemplateProvider : IPostfixTemplateProvider
  {
    public void CreateItems(PostfixTemplateAcceptanceContext context, ICollection<ILookupItem> consumer)
    {
      if (context.CanBeStatement)
      {
        if (context.ExpressionType.IsBool() || context.LooseChecks)
          consumer.Add(new PostfixLookupItemObsolete(context, "ifnot", "if (!$EXPR$) $CARET$"));
      }
    }
  }
}