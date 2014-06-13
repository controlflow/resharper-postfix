using System.Collections.Generic;

namespace JetBrains.ReSharper.PostfixTemplates.Templates
{
  [PostfixTemplate(
    templateName: "null",
    description: "Checks expression to be null",
    example: "if (expr == null)")]
  public class CheckIsNullTemplate : CheckForNullTemplateBase, IPostfixTemplate
  {
    public IPostfixLookupItem CreateItem(PostfixTemplateContext context)
    {
      var outerExpression = context.OuterExpression;
      if (outerExpression != null && outerExpression.CanBeStatement)
      {
        if (IsNullable(outerExpression))
        {
          if (context.IsAutoCompletion && !MakeSenseToCheckInAuto(outerExpression))
            return null; // reduce noise

          return new CheckForNullStatementItem("null", outerExpression, "if($0==null)");
        }
      }
      else if (!context.IsAutoCompletion)
      {
        var nullableExpressions = new List<PrefixExpressionContext>();
        foreach (var expressionContext in context.Expressions)
        {
          if (IsNullable(expressionContext))
            nullableExpressions.Add(expressionContext);
        }

        if (nullableExpressions.Count > 0)
        {
          nullableExpressions.Reverse();
          return new CheckForNullExpressionItem("null", nullableExpressions.ToArray(), "$0==null");
        }
      }

      return null;
    }
  }
}