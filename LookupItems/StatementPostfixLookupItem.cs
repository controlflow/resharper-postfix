using JetBrains.Annotations;
using JetBrains.Application;
using JetBrains.DocumentModel;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.LinqTools;
using JetBrains.ReSharper.Feature.Services.Lookup;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.ExtensionsAPI;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.ReSharper.Psi.Services;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.TextControl;
using System;

namespace JetBrains.ReSharper.ControlFlow.PostfixCompletion.LookupItems
{
  public abstract class StatementPostfixLookupItem<TStatement> : PostfixLookupItem
    where TStatement : class, ICSharpStatement
  {
    protected StatementPostfixLookupItem(
      [NotNull] string shortcut, [NotNull] PrefixExpressionContext context)
      : base(shortcut, context) { }

    protected override bool RemoveSemicolon { get { return true; } }

    protected override void ExpandPostfix(
      ITextControl textControl, Suffix suffix, ISolution solution,
      IPsiModule psiModule, ICSharpExpression expression)
    {
      var factory = CSharpElementFactory.GetInstance(psiModule);
      var newStatement = CreateStatement(factory);

      var statement = expression.GetContainingNode<IStatement>();
      statement.ReplaceBy(newStatement);
    }

    protected virtual bool SuppressSemicolonSuffix { get { return false; } }

    protected virtual void AfterComplete(
      [NotNull] ITextControl textControl, [NotNull] Suffix suffix,
      [NotNull] TStatement statement, int? caretPosition)
    {
      if (SuppressSemicolonSuffix && suffix.HasPresentation && suffix.Presentation == ';')
      {
        suffix = Suffix.Empty;
      }

      AfterComplete(textControl, suffix, caretPosition);
    }

    [NotNull] protected abstract TStatement CreateStatement([NotNull] CSharpElementFactory factory);

    // todo: => PutExpression?
    protected abstract void PlaceExpression(
      [NotNull] TStatement statement, [NotNull] ICSharpExpression expression,
      [NotNull] CSharpElementFactory factory);

    private static bool IsMarkerExpressionStatement(
      [NotNull] IExpressionStatement expressionStatement, [NotNull] string markerName)
    {
      var reference = expressionStatement.Expression as IReferenceExpression;
      return reference != null
          && reference.QualifierExpression == null
          && reference.Delimiter == null
          && reference.NameIdentifier.Name == markerName;
    }
  }
}