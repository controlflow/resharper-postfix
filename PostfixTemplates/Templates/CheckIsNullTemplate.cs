using JetBrains.ReSharper.Feature.Services.Lookup;
using JetBrains.ReSharper.Psi.CSharp.Tree;

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
      if (outerExpression != null && outerExpression.CanBeStatement)
      {
        if (IsNullable(outerExpression))
        {
          if (context.IsAutoCompletion && outerExpression.Expression is IAsExpression)
            return null;

          return new CheckForNullStatementItem("null", outerExpression, "if($0==null)");
        }
      }
      else if (!context.IsAutoCompletion)
      {
        var innerExpression = context.InnerExpression;
        if (IsNullable(innerExpression))
        {
          return new CheckForNullExpressionItem("null", innerExpression, "$0==null");
        }
      }

      return null;
    }
  }
}