using JetBrains.Annotations;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.LookupItems;
using JetBrains.ReSharper.PostfixTemplates.LookupItems;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;

namespace JetBrains.ReSharper.PostfixTemplates.Templates
{
  [PostfixTemplate(
    templateName: "typeof",
    description: "Wraps type usage with typeof() expression",
    example: "typeof(TExpr)")]
  public class TypeOfExpressionTemplate : IPostfixTemplate
  {
    public ILookupItem CreateItem(PostfixTemplateContext context)
    {
      var typeExpression = context.TypeExpression;
      if (typeExpression != null && typeExpression.ReferencedElement is ITypeElement)
      {
        return new TypeOfItem(typeExpression);
      }

      return null;
    }

    private class TypeOfItem : ExpressionPostfixLookupItem<ITypeofExpression>
    {
      public TypeOfItem([NotNull] PrefixExpressionContext context) : base("typeOf", context) { }

      protected override ITypeofExpression CreateExpression(CSharpElementFactory factory, ICSharpExpression expression)
      {
        var template = "typeof(" + expression.GetText() + ")";
        return (ITypeofExpression) factory.CreateExpressionAsIs(template);
      }
    }
  }
}