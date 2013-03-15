using System.Collections.Generic;
using JetBrains.ReSharper.ControlFlow.PostfixCompletion.LookupItems;
using JetBrains.ReSharper.Feature.Services.Lookup;
using JetBrains.ReSharper.Psi.Naming.Impl;
using JetBrains.ReSharper.Psi.Xaml.Impl;

namespace JetBrains.ReSharper.ControlFlow.PostfixCompletion.TemplateProviders
{
  [PostfixTemplateProvider("foreach", "Iterating over expressions of collection type")]
  public class ForEachLoopTemplateProvider : IPostfixTemplateProvider
  {
    public void CreateItems(PostfixTemplateAcceptanceContext context, ICollection<ILookupItem> consumer)
    {
      if (!context.CanBeStatement || context.ExpressionType.IsUnknown) return;

      // todo: check for 'foreach pattern'
      // todo: support untyped collections
      // todo: infer type by indexer like F#

      var predefined = context.Expression.GetPredefinedType();

      var rule = context.Expression.GetTypeConversionRule();
      if (rule.IsImplicitlyConvertibleTo(context.ExpressionType, predefined.IEnumerable))
      {
        consumer.Add(new NameSuggestionPostfixLookupItem(
          context, "foreach", "foreach (var $NAME$ in $EXPR$) $CARET$",
          context.Expression, PluralityKinds.Plural, ScopeKind.LocalSelfScoped));
      }
    }
  }
}