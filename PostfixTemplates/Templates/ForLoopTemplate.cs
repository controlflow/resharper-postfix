using JetBrains.Annotations;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.LookupItems;
using JetBrains.ReSharper.PostfixTemplates.Contexts;
using JetBrains.ReSharper.PostfixTemplates.Contexts.CSharp;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.PostfixTemplates.Templates
{
  // todo: apply code style in R# 9.0

  [PostfixTemplate(
    templateName: "for",
    description: "Iterates over collection with index",
    example: "for (var i = 0; i < xs.Length; i++)")]
  public class ForLoopTemplate : ForLoopTemplateBase, IPostfixTemplate
  {
    public ILookupItem CreateItem(PostfixTemplateContext context)
    {
      string lengthName;
      if (CreateForItem(context, out lengthName))
      {
        var expressionContext = context.InnerExpression;
        if (expressionContext != null)
        {
          return new ForLookupItem(expressionContext, lengthName);
        }
      }

      return null;
    }

    private sealed class ForLookupItem : ForLookupItemBase
    {
      public ForLookupItem([NotNull] CSharpPostfixExpressionContext context, [CanBeNull] string lengthName)
        : base("for", context, lengthName) { }

      protected override IForStatement CreateStatement(CSharpElementFactory factory, ICSharpExpression expression)
      {
        var template = "for(var x=0;x<$0;x++)" + EmbeddedStatementBracesTemplate;
        var forStatement = (IForStatement) factory.CreateStatement(template, expression);

        var condition = (IRelationalExpression) forStatement.Condition;
        if (LengthName == null)
        {
          condition.RightOperand.ReplaceBy(expression);
        }
        else
        {
          var lengthAccess = factory.CreateReferenceExpression("expr.$0", LengthName);
          lengthAccess = condition.RightOperand.ReplaceBy(lengthAccess);
          lengthAccess.QualifierExpression.NotNull().ReplaceBy(expression);
        }

        return forStatement;
      }
    }
  }
}