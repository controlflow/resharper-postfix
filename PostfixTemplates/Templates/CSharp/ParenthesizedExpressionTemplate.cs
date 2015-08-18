using JetBrains.Annotations;
using JetBrains.ReSharper.PostfixTemplates.CodeCompletion;
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
    public PostfixTemplateInfo TryCreateInfo(CSharpPostfixTemplateContext context)
    {
      if (context.IsPreciseMode)
      {
        foreach (var expressionContext in context.Expressions)
        {
          var castExpression = CastExpressionNavigator.GetByOp(expressionContext.Expression);
          if (castExpression == null) continue; // available in auto over cast expressions

          var expression = ParenthesizedExpressionNavigator.GetByExpression(castExpression);
          if (expression != null) continue; // not already parenthesized

          return new PostfixTemplateInfo("par", expressionContext);
        }

        return null;
      }

      var expressions = CommonUtils.FindExpressionWithValuesContexts(context);
      if (expressions.Length != 0)
      {
        return new PostfixTemplateInfo("par", expressions);
      }

      return null;
    }

    public PostfixTemplateBehavior CreateBehavior(PostfixTemplateInfo info)
    {
      return new ParenthesesItem(info);
    }

    private sealed class ParenthesesItem : CSharpExpressionPostfixTemplateBehavior<ICSharpExpression>
    {
      public ParenthesesItem([NotNull] PostfixTemplateInfo info) : base(info) { }

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