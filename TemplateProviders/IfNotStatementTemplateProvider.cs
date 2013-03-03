using System.Collections.Generic;
using JetBrains.ReSharper.ControlFlow.PostfixCompletion.LookupItems;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.ControlFlow.PostfixCompletion.TemplateProviders
{
  [PostfixTemplateProvider("ifnot", "Checks boolean expression to be 'false'")]
  public class IfNotStatementTemplateProvider : IPostfixTemplateProvider
  {
    public IEnumerable<PostfixLookupItem> CreateItems(PostfixTemplateAcceptanceContext context)
    {
      if (context.CanBeStatement)
      {
        if (context.ExpressionType.IsBool() || context.LooseChecks)
          yield return new PostfixLookupItem("ifnot", "if (!$EXPR$) $CARET$");
      }
    }
  }
}