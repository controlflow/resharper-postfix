using JetBrains.Annotations;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.LookupItems;
using JetBrains.ReSharper.PostfixTemplates.CodeCompletion;
using JetBrains.ReSharper.PostfixTemplates.Contexts;
using JetBrains.ReSharper.PostfixTemplates.Contexts.CSharp;
using JetBrains.ReSharper.PostfixTemplates.LookupItems;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;

namespace JetBrains.ReSharper.PostfixTemplates.Templates.CSharp
{
  [PostfixTemplate(
    templateName: "typeof",
    description: "Wraps type usage with typeof() expression",
    example: "typeof(TExpr)")]
  public class TypeOfExpressionTemplate : IPostfixTemplate<CSharpPostfixTemplateContext>
  {
    public PostfixTemplateInfo TryCreateInfo(CSharpPostfixTemplateContext context)
    {
      var typeExpression = context.TypeExpression;
      if (typeExpression == null) return null;

      var typeElement = typeExpression.ReferencedElement as ITypeElement;
      if (typeElement == null) return null;

      return new PostfixTemplateInfo("typeof", typeExpression);
    }

    public PostfixTemplateBehavior CreateBehavior(PostfixTemplateInfo info)
    {
      return new CSharpPostfixTypeOfExpressionBehavior(info);
    }

    private sealed class CSharpPostfixTypeOfExpressionBehavior : CSharpExpressionPostfixTemplateBehavior<ITypeofExpression>
    {
      public CSharpPostfixTypeOfExpressionBehavior([NotNull] PostfixTemplateInfo info) : base(info) { }

      protected override ITypeofExpression CreateExpression(CSharpElementFactory factory, ICSharpExpression expression)
      {
        var template = "typeof(" + expression.GetText() + ")";
        return (ITypeofExpression) factory.CreateExpressionAsIs(template);
      }
    }
  }
}