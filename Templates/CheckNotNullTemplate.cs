using JetBrains.ReSharper.Feature.Services.Lookup;

namespace JetBrains.ReSharper.PostfixTemplates.Templates
{
  [PostfixTemplate(
    templateName: "notnull",
    description: "Checks expression to be not-null",
    example: "if (expr != null)")]
  public class CheckNotNullTemplate : CheckForNullTemplateBase, IPostfixTemplate
  {
    public ILookupItem CreateItem(PostfixTemplateContext context)
    {
      var outerExpression = context.OuterExpression;
      if (outerExpression.CanBeStatement)
      {
        if (IsNullableType(outerExpression.Type))
        {
          return new CheckForNullStatementItem("notNull", outerExpression, "if($0!=null)");
        }
      }
      else if (context.IsForceMode)
      {
        var innerExpression = context.InnerExpression;
        if (IsNullableType(innerExpression.Type))
        {
          return new CheckForNullExpressionItem("notNull", innerExpression, "$0!=null");
        }
      }

      return null;
    }
  }
}