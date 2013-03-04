using System.Collections.Generic;
using JetBrains.ReSharper.ControlFlow.PostfixCompletion.LookupItems;
using JetBrains.ReSharper.Feature.Services.Lookup;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Naming.Impl;
using JetBrains.ReSharper.Psi.Xaml.Impl;
using JetBrains.ReSharper.Psi.Util;

namespace JetBrains.ReSharper.ControlFlow.PostfixCompletion.TemplateProviders
{
  [PostfixTemplateProvider("for", "Iterating over collections with length")]
  public class ForLoopTemplateProvider : IPostfixTemplateProvider
  {
    public void CreateItems(PostfixTemplateAcceptanceContext context, ICollection<ILookupItem> consumer)
    {
      if (!context.CanBeStatement || context.ExpressionType.IsUnknown) return;
      if (!context.Expression.IsPure()) return; // todo: better fix?

      string lengthProperty = null;
      if (context.ExpressionType is IArrayType) lengthProperty = "Length";
      else
      {
        var predefined = context.Expression.GetPredefinedType();
        var rule = context.Expression.GetTypeConversionRule();
        if (rule.IsImplicitlyConvertibleTo(context.ExpressionType, predefined.GenericICollection))
          lengthProperty = "Count";
      }

      if (lengthProperty == null) return;

      var forTemplate = string.Format("for (var $NAME$ = 0; $NAME$ < $EXPR$.{0}; $NAME$++) ", lengthProperty);
      var forrTemplate = string.Format("for (var $NAME$ = $EXPR$.{0}; $NAME$ >= 0; $NAME$--) ", lengthProperty);
      consumer.Add(new NameSuggestionPostfixLookupItem(
                     context, "for", forTemplate, context.Expression,
                     PluralityKinds.Plural, ScopeKind.LocalSelfScoped));
      consumer.Add(new NameSuggestionPostfixLookupItem(
                     context, "forr", forrTemplate, context.Expression,
                     PluralityKinds.Plural, ScopeKind.LocalSelfScoped));
    }
  }
}