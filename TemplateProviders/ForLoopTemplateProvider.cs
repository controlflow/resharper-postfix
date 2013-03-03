using System.Collections.Generic;
using JetBrains.ReSharper.ControlFlow.PostfixCompletion.LookupItems;
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
    public IEnumerable<PostfixLookupItem> CreateItems(PostfixTemplateAcceptanceContext context)
    {
      if (!context.CanBeStatement || context.ExpressionType.IsUnknown) yield break;
      if (!context.Expression.IsPure()) yield break; // todo: better fix?

      string lengthProperty = null;
      if (context.ExpressionType is IArrayType) lengthProperty = "Length";
      else
      {
        var predefined = context.Expression.GetPsiModule().GetPredefinedType();
        var rule = context.Expression.GetTypeConversionRule();
        if (rule.IsImplicitlyConvertibleTo(context.ExpressionType, predefined.GenericICollection))
          lengthProperty = "Count";
      }

      if (lengthProperty != null)
      {
        var forTemplate = string.Format("for (var $NAME$ = 0; $NAME$ < $EXPR$.{0}; $NAME$++) $CARET$", lengthProperty);
        var forrTemplate = string.Format("for (var $NAME$ = $EXPR$.{0}; $NAME$ >= 0; $NAME$--) $CARET$", lengthProperty);
        yield return new NameSuggestionPostfixLookupItem(
          "for", forTemplate, context.Expression, PluralityKinds.Plural, ScopeKind.LocalSelfScoped);
        yield return new NameSuggestionPostfixLookupItem(
          "forr", forrTemplate, context.Expression, PluralityKinds.Plural, ScopeKind.LocalSelfScoped);
      }
    }
  }
}