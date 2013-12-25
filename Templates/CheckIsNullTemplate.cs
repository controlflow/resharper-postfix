using JetBrains.ReSharper.Feature.Services.Lookup;

namespace JetBrains.ReSharper.PostfixTemplates.Templates
{
  [PostfixTemplate(
    templateName: "null",
    description: "Checks expression to be null",
    example: "if (expr == null)")]
  public class CheckIsNullTemplate : CheckForNullTemplateBase, IPostfixTemplate
  {
    public ILookupItem CreateItem(PostfixTemplateContext context)
    {
      var outerExpression = context.OuterExpression;
      if (outerExpression.CanBeStatement)
      {
        if (IsNullableType(outerExpression.Type))
        {
          return new CheckForNullStatementItem("null", outerExpression, "if($0==null)");
        }
      }
      else if (context.IsForceMode)
      {
        var innerExpression = context.InnerExpression;
        if (IsNullableType(innerExpression.Type))
        {
          return new CheckForNullExpressionItem("null", innerExpression, "$0==null");
        }
      }

      return null;
    }
  }
}