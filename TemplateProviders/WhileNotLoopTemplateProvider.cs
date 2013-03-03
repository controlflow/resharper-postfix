using System.Collections.Generic;
using JetBrains.ReSharper.ControlFlow.PostfixCompletion.LookupItems;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.ControlFlow.PostfixCompletion.TemplateProviders
{
  [PostfixTemplateProvider("whilenot", "Iterating while boolean statement is not true")]
  public class WhileNotLoopTemplateProvider : IPostfixTemplateProvider
  {
    public IEnumerable<PostfixLookupItem> CreateItems(PostfixTemplateAcceptanceContext context)
    {
      if (context.CanBeStatement)
      {
        if (context.LooseChecks || context.ExpressionType.IsBool())
          yield return new PostfixLookupItem("whilenot", "while (!$EXPR$) $CARET$");
      }
    }
  }
}