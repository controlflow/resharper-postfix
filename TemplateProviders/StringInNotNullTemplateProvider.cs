using System.Collections.Generic;
using JetBrains.ReSharper.ControlFlow.PostfixCompletion.LookupItems;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.ControlFlow.PostfixCompletion.TemplateProviders
{
  [PostfixTemplateProvider("ifempty", "Checks string for null or empty string")]
  public class StringInNotNullTemplateProvider : IPostfixTemplateProvider
  {
    public IEnumerable<PostfixLookupItem> CreateItems(PostfixTemplateAcceptanceContext context)
    {
      if (context.ExpressionType.IsString())
      {
        if (context.CanBeStatement)
        {
          yield return new PostfixLookupItem("ifempty", "if (string.IsNullOrEmpty($EXPR$)) ");
        }
        else
        {
          yield return new PostfixLookupItem("ifempty", "string.IsNullOrEmpty($EXPR$)");
        }
      }
    }
  }
}