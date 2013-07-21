using JetBrains.Annotations;
using JetBrains.Application.Settings;
using JetBrains.ReSharper.ControlFlow.PostfixCompletion.Settings;
using JetBrains.ReSharper.Feature.Services.Lookup;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.TextControl;
#if RESHARPER7
using JetBrains.ReSharper.Psi;
#else
using JetBrains.ReSharper.Psi.Modules;
#endif

namespace JetBrains.ReSharper.ControlFlow.PostfixCompletion.LookupItems
{
  internal abstract class KeywordStatementPostfixLookupItem<TStatement>
    : StatementPostfixLookupItem<TStatement>
    where TStatement : class, ICSharpStatement
  {
    protected KeywordStatementPostfixLookupItem(
      [NotNull] string shortcut, [NotNull] PrefixExpressionContext context)
      : base(shortcut, context)
    {
      BracesInsertion = context.Expression.GetSettingsStore().GetValue(
        PostfixCompletionSettingsAccessor.UseBracesForEmbeddedStatements);
    }

    protected bool BracesInsertion { get; set; }
    protected abstract string Keyword { get; }

    protected override TStatement CreateStatement(
      IPsiModule psiModule, CSharpElementFactory factory)
    {
      var template = Keyword + (BracesInsertion
        ? "(expr){" + CaretMarker + ";}"
        : "(expr)" + CaretMarker + ";");

      return (TStatement) factory.CreateStatement(template);
    }

    // force inheritors to override
    protected abstract override void PlaceExpression(
      TStatement statement, ICSharpExpression expression, CSharpElementFactory factory);

    protected override void ReplaySuffix(ITextControl textControl, Suffix suffix)
    {
      if (BracesInsertion && suffix.HasPresentation && suffix.Presentation == '{')
        return;

      base.ReplaySuffix(textControl, suffix);
    }
  }
}