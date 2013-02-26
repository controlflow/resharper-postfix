using System.Collections.Generic;
using JetBrains.ReSharper.ControlFlow.PostfixCompletion.LookupItems;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Naming.Impl;
using JetBrains.ReSharper.Psi.Xaml.Impl;
using JetBrains.ReSharper.Psi.Util;

namespace JetBrains.ReSharper.ControlFlow.PostfixCompletion.TemplateProviders
{
  [PostfixTemplateProvider("for", "Iterating collections with length")]
  public class ForLoopTemplateProvider : IPostfixTemplateProvider
  {
    public IEnumerable<PostfixLookupItem> CreateItems(
      IReferenceExpression referenceExpression, ICSharpExpression expression, IType expressionType, bool canBeStatement)
    {
      if (!canBeStatement || expressionType.IsUnknown || !expression.IsPure()) yield break;

      string lengthProperty = null;
      if (expressionType is IArrayType) lengthProperty = "Length";
      else
      {
        var predefined = expression.GetPsiModule().GetPredefinedType();
        var rule = expression.GetTypeConversionRule();
        if (rule.IsImplicitlyConvertibleTo(expressionType, predefined.GenericICollection))
          lengthProperty = "Count";
      }

      if (lengthProperty != null)
      {
        var forTemplate = string.Format("for (var $NAME$ = 0; $NAME$ < $EXPR$.{0}; $NAME$++) $CARET$", lengthProperty);
        var forrTemplate = string.Format("for (var $NAME$ = $EXPR$.{0}; $NAME$ >= 0; $NAME$--) $CARET$", lengthProperty);
        yield return new NameSuggestionPostfixLookupItem(
          "for", forTemplate, expression, PluralityKinds.Plural, ScopeKind.LocalSelfScoped);
        yield return new NameSuggestionPostfixLookupItem(
          "forr", forrTemplate, expression, PluralityKinds.Plural, ScopeKind.LocalSelfScoped);
      }
    }
  }
}