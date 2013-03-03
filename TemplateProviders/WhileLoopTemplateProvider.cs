using System.Collections.Generic;
using JetBrains.ReSharper.ControlFlow.PostfixCompletion.LookupItems;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.ControlFlow.PostfixCompletion.TemplateProviders
{
  [PostfixTemplateProvider("while", "Iterating while boolean statement is true")]
  public class WhileLoopTemplateProvider : IPostfixTemplateProvider
  {
    public IEnumerable<PostfixLookupItem> CreateItems(PostfixTemplateAcceptanceContext context)
    {
      if (context.CanBeStatement)
      {
        if (context.LooseChecks || context.ExpressionType.IsBool())
          yield return new PostfixLookupItem("while", "while ($EXPR$) $CARET$");
      }
    }
  }
}