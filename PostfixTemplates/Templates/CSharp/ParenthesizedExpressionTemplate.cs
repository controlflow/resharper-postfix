using JetBrains.Annotations;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.LookupItems;
using JetBrains.ReSharper.PostfixTemplates.Contexts;
using JetBrains.ReSharper.PostfixTemplates.Contexts.CSharp;
using JetBrains.ReSharper.PostfixTemplates.LookupItems;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;

namespace JetBrains.ReSharper.PostfixTemplates.Templates.CSharp
{
  // todo: return foo.Bar as T.par do not works (R# bug)

  [PostfixTemplate(
    templateName: "par",
    description: "Parenthesizes current expression",
    example: "(expr)")]
  public class ParenthesizedExpressionTemplate : IPostfixTemplate<CSharpPostfixTemplateContext>
  {
    public ILookupItem CreateItem(CSharpPostfixTemplateContext context)
    {
      if (context.IsPreciseMode)
      {
        foreach (var expressionContext in context.Expressions)
        {
          var castExpression = CastExpressionNavigator.GetByOp(expressionContext.Expression);
          if (castExpression == null) continue; // available in auto over cast expressions

          var expression = ParenthesizedExpressionNavigator.GetByExpression(castExpression);
          if (expression != null) continue; // not already parenthesized

          return new ParenthesesItem(expressionContext);
        }

        return null;
      }

      var contexts = CommonUtils.FindExpressionWithValuesContexts(context);
      if (contexts.Length == 0) return null;

      return new ParenthesesItem(contexts);
    }

    private sealed class ParenthesesItem : ExpressionPostfixLookupItem<ICSharpExpression>
    {
      public ParenthesesItem([NotNull] params CSharpPostfixExpressionContext[] contexts)
        : base("par", contexts) { }

      protected override string ExpressionSelectTitle
      {
        get { return "Select expression to parenthesize"; }
      }

      protected override ICSharpExpression CreateExpression(CSharpElementFactory factory, ICSharpExpression expression)
      {
        return factory.CreateExpression("($0)", expression);
      }
    }
  }
}