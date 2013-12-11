using JetBrains.Annotations;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Lookup;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.TextControl;
using JetBrains.ReSharper.Psi.Modules;

namespace JetBrains.ReSharper.ControlFlow.PostfixCompletion.LookupItems
{
  public abstract class ExpressionPostfixLookupItem<TExpression> : PostfixLookupItem
    where TExpression : class, ICSharpExpression
  {
    protected ExpressionPostfixLookupItem(
      [NotNull] string shortcut, [NotNull] PrefixExpressionContext context)
      : base(shortcut, context) { }

    protected override void ExpandPostfix(
      ITextControl textControl, Suffix suffix, ISolution solution,
      IPsiModule psiModule, ICSharpExpression expression)
    {
      
    }

    protected virtual void AfterComplete(
      [NotNull] ITextControl textControl, [NotNull] Suffix suffix,
      [NotNull] TExpression expression, int? caretPosition)
    {
      AfterComplete(textControl, suffix, caretPosition);
    }

    [NotNull] protected abstract TExpression CreateExpression(
      [NotNull] CSharpElementFactory factory, [NotNull] ICSharpExpression expression);

    private static bool IsMarkerExpression(
      [NotNull] ICSharpExpression expression, [NotNull] string markerName)
    {
      var reference = expression as IReferenceExpression;
      return reference != null
          && reference.QualifierExpression == null
          && reference.Delimiter == null
          && reference.NameIdentifier.Name == markerName;
    }
  }
}