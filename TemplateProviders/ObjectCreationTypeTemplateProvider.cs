using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.ReSharper.ControlFlow.PostfixCompletion.LookupItems;
using JetBrains.ReSharper.Feature.Services.Lookup;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Util;
#if RESHARPER8
using JetBrains.ReSharper.Psi.Modules;
#endif

namespace JetBrains.ReSharper.ControlFlow.PostfixCompletion.TemplateProviders
{
  [PostfixTemplateProvider("new", "Invokes the constructor of type", WorksOnTypes = true)]
  public class ObjectCreationTypeTemplateProvider : IPostfixTemplateProvider
  {
    public void CreateItems(PostfixTemplateAcceptanceContext context, ICollection<ILookupItem> consumer)
    {
      var exprContext = context.PossibleExpressions.FirstOrDefault();
      if (exprContext == null) return;

      var typeElement = exprContext.ReferencedElement as ITypeElement;
      if (typeElement is IStruct || typeElement is IEnum || typeElement is IClass)
      {
        // filter out abstract classes
        var classType = typeElement as IClass;
        if (classType != null && classType.IsAbstract) return;
  
        // check type has any constructor accessable
        var accessContext = new ElementAccessContext(exprContext.Expression);
        foreach (var constructor in typeElement.Constructors)
        {
          if (!constructor.IsStatic && AccessUtil.IsSymbolAccessible(constructor, accessContext))
          {
            consumer.Add(new LookupItem(exprContext));
            break;
          }
        }
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
        IPsiModule psiModule, CSharpElementFactory factory, ICSharpExpression expression)
      {
        return (IObjectCreationExpression)
          factory.CreateExpression("new $0(" + CaretMarker + ")", myTypeText);
      }
    }
  }
}