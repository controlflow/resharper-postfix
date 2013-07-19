using System.Collections.Generic;
using JetBrains.ReSharper.ControlFlow.PostfixCompletion.LookupItems;
using JetBrains.ReSharper.Feature.Services.Lookup;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.ControlFlow.PostfixCompletion.TemplateProviders
{
  //[PostfixTemplateProvider("whilenot", "Iterating while boolean statement is not true")]
  public class WhileNotLoopTemplateProvider : IPostfixTemplateProvider
  {
    public void CreateItems(PostfixTemplateAcceptanceContext context, ICollection<ILookupItem> consumer)
    {
      if (context.CanBeStatement)
      {
        if (context.ForceMode || context.ExpressionType.IsBool())
          consumer.Add(new PostfixLookupItemObsolete(context, "whilenot", "while (!$EXPR$) $CARET$"));
      }
    }
  }
}