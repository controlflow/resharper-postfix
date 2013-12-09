using JetBrains.Annotations;
using JetBrains.ReSharper.Feature.Services.Lookup;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.ControlFlow.PostfixCompletion.TemplateProviders
{
  [PostfixTemplate(
    templateName: "forr",
    description: "Iterates over collection in reverse with index",
    example: "for (var i = expr.Length - 1; i >= 0; i--)")]
  public class ForReverseLoopTemplate : ForLoopTemplateBase, IPostfixTemplate {
    public ILookupItem CreateItems(PostfixTemplateContext context) {
      string lengthPropertyName;
      if (CreateItems(context, out lengthPropertyName)) {
        return new ReverseForLookupItem(context.InnerExpression, lengthPropertyName);
      }

      return null;
    }

    private sealed class ReverseForLookupItem : ForLookupItemBase {
      public ReverseForLookupItem([NotNull] PrefixExpressionContext context,
                                  [CanBeNull] string lengthPropertyName)
        : base("forR", context, lengthPropertyName) { }

      protected override string Template {
        get { return "for(var x=expr-1;x>=0;x--)"; }
      }

      protected override void PlaceExpression(IForStatement forStatement,
                                              ICSharpExpression expression,
                                              CSharpElementFactory factory) {
        var variable = (ILocalVariableDeclaration)forStatement.Initializer.Declaration.Declarators[0];
        var initializer = (IExpressionInitializer)variable.Initial;

        if (LengthPropertyName == null) {
          var value = initializer.Value.ReplaceBy(expression);
          value.ReplaceBy(factory.CreateExpression("$0 - 1", value));
        } else {
          var lengthAccess = factory.CreateReferenceExpression("expr.$0", LengthPropertyName);
          lengthAccess = initializer.Value.ReplaceBy(lengthAccess);
          lengthAccess.QualifierExpression.NotNull().ReplaceBy(expression);
          lengthAccess.ReplaceBy(factory.CreateExpression("$0 - 1", lengthAccess));
        }
      }
    }
  }
}
