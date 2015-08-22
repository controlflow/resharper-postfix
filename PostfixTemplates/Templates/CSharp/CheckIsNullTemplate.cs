using System.Collections.Generic;
using JetBrains.ReSharper.PostfixTemplates.Contexts.CSharp;
using JetBrains.ReSharper.PostfixTemplates.LookupItems;

namespace JetBrains.ReSharper.PostfixTemplates.Templates.CSharp
{
  [PostfixTemplate(
    templateName: "null",
    description: "Checks expression to be null",
    example: "if (expr == null)")]
  public class CheckIsNullTemplate : CheckForNullTemplateBase
  {
    protected override CheckForNullPostfixTemplateInfo TryCreateInfo(CSharpPostfixTemplateContext context)
    {
      var outerExpression = context.OuterExpression;
      if (outerExpression != null && outerExpression.CanBeStatement)
      {
        if (IsNullable(outerExpression))
        {
          if (context.IsPreciseMode && !MakeSenseToCheckInPreciseMode(outerExpression))
            return null; // reduce noise

          return new CheckForNullPostfixTemplateInfo(
            "null", outerExpression, checkNotNull: false, target: PostfixTemplateTarget.Statement);
        }
      }
      else if (!context.IsPreciseMode)
      {
        var nullableExpressions = new List<CSharpPostfixExpressionContext>();
        foreach (var expressionContext in context.Expressions)
        {
          if (IsNullable(expressionContext))
            nullableExpressions.Add(expressionContext);
        }

        if (nullableExpressions.Count > 0)
        {
          nullableExpressions.Reverse();

          return new CheckForNullPostfixTemplateInfo(
            "null", nullableExpressions, checkNotNull: false, target: PostfixTemplateTarget.Expression);
        }
      }

      return null;
    }
  }
}