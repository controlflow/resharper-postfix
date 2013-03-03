using System.Collections.Generic;
using JetBrains.ReSharper.ControlFlow.PostfixCompletion.LookupItems;
using JetBrains.ReSharper.Feature.Services.Lookup;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.ControlFlow.PostfixCompletion.TemplateProviders
{
  [PostfixTemplateProvider("if", "Checks boolean expression to be 'true'")]
  public class IfStatementTemplateProvider : IPostfixTemplateProvider
  {
    // todo: detect relational expressions

    public void CreateItems(PostfixTemplateAcceptanceContext context, ICollection<ILookupItem> consumer)
    {
      if (context.CanBeStatement)
      {
        // todo: smart caret? stay in condition when loose?

        if (context.ExpressionType.IsBool() || context.LooseChecks)
          consumer.Add(new PostfixLookupItem(context, "if", "if ($EXPR$) $CARET$"));
      }
    }
  }
}