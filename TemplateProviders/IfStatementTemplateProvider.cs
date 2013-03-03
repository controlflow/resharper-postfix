using System.Collections.Generic;
using JetBrains.ReSharper.ControlFlow.PostfixCompletion.LookupItems;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.ControlFlow.PostfixCompletion.TemplateProviders
{
  [PostfixTemplateProvider("if", "Checks boolean expression to be 'true'")]
  public class IfStatementTemplateProvider : IPostfixTemplateProvider
  {
    public IEnumerable<PostfixLookupItem> CreateItems(PostfixTemplateAcceptanceContext context)
    {
      if (context.CanBeStatement)
      {
        if (context.ExpressionType.IsBool() || context.LooseChecks)
          yield return new PostfixLookupItem("if", "if ($EXPR$) $CARET$");
      }
    }
  }
}