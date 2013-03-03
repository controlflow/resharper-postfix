using System.Collections.Generic;
using JetBrains.ReSharper.ControlFlow.PostfixCompletion.LookupItems;
using JetBrains.ReSharper.Feature.Services.Lookup;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.ControlFlow.PostfixCompletion.TemplateProviders
{
  [PostfixTemplateProvider("list", "Wraps type as generic list element type", WorksOnTypes = true)]
  public class ListTypeExpressionTemplateProvider : IPostfixTemplateProvider
  {
    public void CreateItems(PostfixTemplateAcceptanceContext context, ICollection<ILookupItem> consumer)
    {
      var typeElement = context.ExpressionReferencedElement as ITypeElement;
      if (typeElement != null)
      {
        consumer.Add(new PostfixLookupItem(context, "list", "List<$EXPR$>$CARET$"));
      }
    }
  }
}