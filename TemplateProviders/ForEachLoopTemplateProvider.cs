using System.Collections.Generic;
using JetBrains.ReSharper.ControlFlow.PostfixCompletion.LookupItems;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Naming.Impl;
using JetBrains.ReSharper.Psi.Xaml.Impl;

namespace JetBrains.ReSharper.ControlFlow.PostfixCompletion.TemplateProviders
{
  [PostfixTemplateProvider("foreach", "Iterating over expressions of collection type")]
  public class ForEachLoopTemplateProvider : IPostfixTemplateProvider
  {
    public IEnumerable<PostfixLookupItem> CreateItems(
      ICSharpExpression expression, IType expressionType, bool canBeStatement)
    {
      if (!canBeStatement || expressionType.IsUnknown) yield break;

      // todo: check for 'foreach pattern'
      // todo: support untyped collections
      // todo: infer type by indexer like F#

      var predefined = expression.GetPsiModule().GetPredefinedType();

      var rule = expression.GetTypeConversionRule();
      if (rule.IsImplicitlyConvertibleTo(expressionType, predefined.IEnumerable))
      {
        yield return new NameSuggestionPostfixLookupItem(
          "foreach", "foreach (var $NAME$ in $EXPR$) $CARET$",
          expression, PluralityKinds.Plural, ScopeKind.LocalSelfScoped);
      }
    }
  }
}