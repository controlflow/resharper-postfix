using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.ReSharper.ControlFlow.PostfixCompletion.LookupItems;
using JetBrains.ReSharper.Feature.Services.Lookup;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Xaml.Impl;
#if RESHARPER7
using JetBrains.ReSharper.Psi;
#else
using JetBrains.ReSharper.Psi.Modules;
#endif

namespace JetBrains.ReSharper.ControlFlow.PostfixCompletion.TemplateProviders
{
  // todo: check for 'foreach pattern'
  // todo: support untyped collections
  // todo: infer type by indexer like F#

  [PostfixTemplateProvider("foreach", "Iterating over expressions of collection type")]
  public class ForEachLoopTemplateProvider : IPostfixTemplateProvider
  {
    public void CreateItems(PostfixTemplateAcceptanceContext context, ICollection<ILookupItem> consumer)
    {
      var exprContext = context.PossibleExpressions.LastOrDefault();
      if (exprContext == null || !exprContext.CanBeStatement) return;

      var typeIsEnumerable = context.ForceMode;
      if (!typeIsEnumerable)
      {
        var predefined = exprContext.Expression.GetPredefinedType();
        var rule = exprContext.Expression.GetTypeConversionRule();
        if (rule.IsImplicitlyConvertibleTo(exprContext.Type, predefined.IEnumerable))
        {
          typeIsEnumerable = true;
        }
      }

      if (typeIsEnumerable)
      {
        consumer.Add(new LookupItem(exprContext));
      }
    }

    private sealed class LookupItem : KeywordStatementPostfixLookupItem<IForeachStatement>
    {
      public LookupItem([NotNull] PrefixExpressionContext context) : base("foreach", context) { }

      protected override string Keyword { get { return "foreach"; } }
      public override bool ShortcutIsCSharpStatementKeyword { get { return true; } }

      protected override IForeachStatement CreateStatement(IPsiModule psiModule, CSharpElementFactory factory)
      {
        var template = BracesInsertion
        ? Keyword + "(var x in expr){" + CaretMarker + ";}"
        : Keyword + "(var x in expr)" + CaretMarker + ";";

        return (IForeachStatement) factory.CreateStatement(template);
      }

      protected override void PlaceExpression(
        IForeachStatement statement, ICSharpExpression expression, CSharpElementFactory factory)
      {
        statement.Collection.ReplaceBy(expression);
      }
    }
  }
}