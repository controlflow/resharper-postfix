using System.Collections.Generic;
using JetBrains.ReSharper.ControlFlow.PostfixCompletion.LookupItems;
using JetBrains.ReSharper.Feature.Services.Lookup;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.ControlFlow.PostfixCompletion.TemplateProviders
{
  //[PostfixTemplateProvider("ifempty", "Checks string for null or empty string")]
  public class StringInNotNullTemplateProvider : IPostfixTemplateProvider
  {
    public void CreateItems(PostfixTemplateAcceptanceContext context, ICollection<ILookupItem> consumer)
    {
      if (context.ExpressionType.IsString())
      {
        consumer.Add(context.CanBeStatement
          ? new PostfixLookupItemObsolete(context, "ifempty", "if (string.IsNullOrEmpty($EXPR$)) ")
          : new PostfixLookupItemObsolete(context, "ifempty", "string.IsNullOrEmpty($EXPR$)"));
      }
    }
  }
}