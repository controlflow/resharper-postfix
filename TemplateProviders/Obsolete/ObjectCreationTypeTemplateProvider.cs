using System.Collections.Generic;
using JetBrains.ReSharper.ControlFlow.PostfixCompletion.LookupItems;
using JetBrains.ReSharper.Feature.Services.Lookup;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Util;

namespace JetBrains.ReSharper.ControlFlow.PostfixCompletion.TemplateProviders
{
  //[PostfixTemplateProvider("new", "Invokes the constructor of type", WorksOnTypes = true)]
  public class ObjectCreationTypeTemplateProvider : IPostfixTemplateProvider
  {
    public void CreateItems(PostfixTemplateAcceptanceContext context, ICollection<ILookupItem> consumer)
    {


      var typeElement = context.ReferencedElement as ITypeElement;
      if (typeElement is IStruct || typeElement is IEnum || typeElement is IClass)
      {
        // filter out abstract classes
        var classType = typeElement as IClass;
        if (classType != null && classType.IsAbstract) return;

        // check type has any constructor accessable
        var accessContext = new ElementAccessContext(context.Expression);
        foreach (var constructor in typeElement.Constructors)
        {
          if (!constructor.IsStatic && AccessUtil.IsSymbolAccessible(constructor, accessContext))
          {
            consumer.Add(new PostfixLookupItemObsolete(context, "new", "new $EXPR$($CARET$)"));
            break;
          }
        }
      }
    }
  }
}