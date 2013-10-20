using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.ReSharper.ControlFlow.PostfixCompletion.LookupItems;
using JetBrains.ReSharper.Feature.Services.Lookup;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;

namespace JetBrains.ReSharper.ControlFlow.PostfixCompletion.TemplateProviders
{
  [PostfixTemplateProvider(
    templateName: "typeof",
    description: "Wraps typw with typeof-expression",
    example: "typeof(TExpr)", WorksOnTypes = true)]
  public class TypeOfExpressionTemplateProvider : IPostfixTemplateProvider
  {
    public void CreateItems(PostfixTemplateAcceptanceContext context, ICollection<ILookupItem> consumer)
    {
      var expression = context.InnerExpression;
      if (expression.ReferencedElement is ITypeElement)
      {
        consumer.Add(new LookupItem(expression));
      }
    }

    private class LookupItem : ExpressionPostfixLookupItem<ITypeofExpression>
    {
      public LookupItem([NotNull] PrefixExpressionContext context)
        : base("typeOf", context) { }

      protected override ITypeofExpression CreateExpression(
        CSharpElementFactory factory, ICSharpExpression expression)
      {
        var template = "typeof(" + expression.GetText() + ")";
        return (ITypeofExpression) factory.CreateExpressionAsIs(template);
      }
    }
  }
}