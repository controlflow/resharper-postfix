using JetBrains.Annotations;
using JetBrains.ReSharper.ControlFlow.PostfixCompletion.LookupItems;
using JetBrains.ReSharper.Feature.Services.Lookup;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;

namespace JetBrains.ReSharper.ControlFlow.PostfixCompletion.TemplateProviders
{
  [PostfixTemplate(
    templateName: "typeof",
    description: "Wraps type usage with typeof() expression",
    example: "typeof(TExpr)", WorksOnTypes = true)]
  public class TypeOfExpressionTemplate : IPostfixTemplate {
    public ILookupItem CreateItems(PostfixTemplateContext context) {
      var expressionContext = context.InnerExpression;
      if (expressionContext.ReferencedElement is ITypeElement) {
        return new LookupItem(expressionContext);
      }

      return null;
    }

    private class LookupItem : ExpressionPostfixLookupItem<ITypeofExpression> {
      public LookupItem([NotNull] PrefixExpressionContext context) : base("typeOf", context) { }

      protected override ITypeofExpression CreateExpression(CSharpElementFactory factory,
                                                            ICSharpExpression expression) {
        var template = "typeof(" + expression.GetText() + ")";
        return (ITypeofExpression) factory.CreateExpressionAsIs(template);
      }
    }
  }
}