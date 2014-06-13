using JetBrains.Annotations;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.PostfixTemplates.Templates
{
  [PostfixTemplate(
    templateName: "forr",
    description: "Iterates over collection in reverse with index",
    example: "for (var i = xs.Length-1; i >= 0; i--)")]
  public class ForReverseLoopTemplate : ForLoopTemplateBase, IPostfixTemplate
  {
    public IPostfixLookupItem CreateItem(PostfixTemplateContext context)
    {
      string lengthName;
      if (CreateForItem(context, out lengthName))
      {
        var expressionContext = context.InnerExpression;
        if (expressionContext != null)
        {
          return new ReverseForLookupItem(expressionContext, lengthName);
        }
      }

      return null;
    }

    private sealed class ReverseForLookupItem : ForLookupItemBase
    {
      public ReverseForLookupItem([NotNull] PrefixExpressionContext context,
                                  [CanBeNull] string lengthName)
        : base("forR", context, lengthName) { }

      protected override IForStatement CreateStatement(CSharpElementFactory factory,
                                                       ICSharpExpression expression)
      {
        var hasLength = (LengthName != null);
        var template = hasLength ? "for(var x=$0;x>=0;x--)" : "for(var x=$0;x>0;x--)";
        var forStatement = (IForStatement) factory.CreateStatement(
          template + EmbeddedStatementBracesTemplate, expression);

        var variable = (ILocalVariableDeclaration) forStatement.Initializer.Declaration.Declarators[0];
        var initializer = (IExpressionInitializer) variable.Initial;

        if (!hasLength)
        {
          var value = initializer.Value.ReplaceBy(expression);
          value.ReplaceBy(value);
        }
        else
        {
          var lengthAccess = factory.CreateReferenceExpression("expr.$0", LengthName);
          lengthAccess = initializer.Value.ReplaceBy(lengthAccess);
          lengthAccess.QualifierExpression.NotNull().ReplaceBy(expression);
          lengthAccess.ReplaceBy(factory.CreateExpression("$0 - 1", lengthAccess));
        }

        return forStatement;
      }
    }
  }
}