using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.ReSharper.ControlFlow.PostfixCompletion.LookupItems;
using JetBrains.ReSharper.Feature.Services.Lookup;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Util;

namespace JetBrains.ReSharper.ControlFlow.PostfixCompletion.TemplateProviders
{
  // todo: show parameter info
  // todo: can create array?

  [PostfixTemplateProvider(
    templateName: "new",
    description: "Produces instantiation expression for type",
    example: "new SomeType()", WorksOnTypes = true)]
  public class ObjectCreationTypeTemplateProvider : IPostfixTemplateProvider
  {
    public void CreateItems(PostfixTemplateAcceptanceContext context, ICollection<ILookupItem> consumer)
    {
      var exprContext = context.InnerExpression;

      var typeElement = exprContext.ReferencedElement as ITypeElement;
      if (typeElement != null && CommonUtils.IsInstantiable(typeElement, exprContext.Expression))
      {
        consumer.Add(new LookupItem(exprContext));
      }
    }

    private sealed class LookupItem : ExpressionPostfixLookupItem<IObjectCreationExpression>
    {
      [NotNull] private readonly string myTypeText;

      public LookupItem([NotNull] PrefixExpressionContext context) : base("new", context)
      {
        myTypeText = context.Expression.GetText();
      }

      protected override IObjectCreationExpression CreateExpression(
        CSharpElementFactory factory, ICSharpExpression expression)
      {
        return (IObjectCreationExpression)
          factory.CreateExpression("new $0(" + CaretMarker + ")", myTypeText);
      }
    }
  }
}