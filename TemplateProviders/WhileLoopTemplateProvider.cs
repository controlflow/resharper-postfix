using System.Collections.Generic;
using JetBrains.ReSharper.ControlFlow.PostfixCompletion.LookupItems;
using JetBrains.ReSharper.Feature.Services.Lookup;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.ControlFlow.PostfixCompletion.TemplateProviders
{
  [PostfixTemplateProvider("while", "Iterating while boolean statement is true")]
  public class WhileLoopTemplateProvider : IPostfixTemplateProvider
  {
    public void CreateItems(PostfixTemplateAcceptanceContext context, ICollection<ILookupItem> consumer)
    {
      if (context.CanBeStatement)
      {
        if (context.LooseChecks || context.ExpressionType.IsBool())
          consumer.Add(new PostfixLookupItem(context, "while", "while ($EXPR$) $CARET$"));
      }
    }
  }
}