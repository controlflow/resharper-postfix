using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.ReSharper.ControlFlow.PostfixCompletion.LookupItems;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.Hotspots;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.LiveTemplates;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.Macros;
using JetBrains.ReSharper.Feature.Services.Lookup;
using JetBrains.ReSharper.LiveTemplates;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.ReSharper.Psi.Xaml.Impl;
using JetBrains.TextControl;
using JetBrains.Util;
#if RESHARPER8
using JetBrains.ReSharper.Feature.Services.LiveTemplates.Macros.Implementations;
#endif

namespace JetBrains.ReSharper.ControlFlow.PostfixCompletion.TemplateProviders
{
  // todo: check ..using bug

  [PostfixTemplateProvider("using", "Wrap resource with using statement")]
  public class UsingStatementTemplateProvider : IPostfixTemplateProvider
  {
    // todo: available here: var alreadyHasVar = smth.using

    public void CreateItems(PostfixTemplateAcceptanceContext context, ICollection<ILookupItem> consumer)
    {
      var exprContext = context.OuterExpression;
      if (!exprContext.CanBeStatement) return;

      if (!context.ForceMode)
      {
        var predefined = exprContext.Expression.GetPredefinedType();
        var rule = exprContext.Expression.GetTypeConversionRule();
        if (!rule.IsImplicitlyConvertibleTo(exprContext.Type, predefined.IDisposable))
          return;
      }

      // check expression is local variable reference
      ILocalVariable usingVar = null;
      var expr = exprContext.Expression as IReferenceExpression;
      if (expr != null && expr.QualifierExpression == null)
        usingVar = expr.Reference.Resolve().DeclaredElement as ILocalVariable;

      ITreeNode node = exprContext.Expression;
      while (true) // inspect containing using statements
      {
        var usingStatement = node.GetContainingNode<IUsingStatement>();
        if (usingStatement == null) break;

        // check if expressions is variable declared with using statement
        var declaration = usingStatement.Declaration;
        if (usingVar != null && declaration != null)
          foreach (var member in declaration.DeclaratorsEnumerable)
            if (Equals(member.DeclaredElement, usingVar))
              return;

        // check expression is already in using statement expression
        if (declaration == null)
          foreach (var e in usingStatement.ExpressionsEnumerable)
            if (MiscUtil.AreExpressionsEquivalent(e, exprContext.Expression))
              return;

        node = usingStatement;
      }

      consumer.Add(new LookupItem(exprContext));
    }

    private sealed class LookupItem : KeywordStatementPostfixLookupItem<IUsingStatement>
    {
      public LookupItem([NotNull] PrefixExpressionContext context) : base("using", context) { }

      protected override string Template { get { return "using(T x = expr)"; } }

      protected override void PlaceExpression(
        IUsingStatement statement, ICSharpExpression expression, CSharpElementFactory factory)
      {
        var declaration = (ILocalVariableDeclaration) statement.Declaration.Declarators[0];
        var initializer = (IExpressionInitializer) declaration.Initial;

        initializer.Value.ReplaceBy(expression);
      }

      protected override void AfterComplete(
        ITextControl textControl, Suffix suffix, IUsingStatement statement, int? caretPosition)
      {
        if (caretPosition == null) return;

        var declaration = (ILocalVariableDeclaration) statement.Declaration.Declarators[0];

#if RESHARPER7
        var typeExpression = new MacroCallExpression(new SuggestVariableTypeMacro());
        var nameExpression = new MacroCallExpression(new SuggestVariableNameMacro());
#else
        var typeExpression = new MacroCallExpressionNew(new SuggestVariableTypeMacroDef());
        var nameExpression = new MacroCallExpressionNew(new SuggestVariableNameMacroDef());
#endif

        var typeSpot = new HotspotInfo(
          new TemplateField("type", typeExpression, 0),
          declaration.TypeUsage.GetDocumentRange().GetHotspotRange());

        var nameSpot = new HotspotInfo(
          new TemplateField("name", nameExpression, 0),
          declaration.NameIdentifier.GetDocumentRange().GetHotspotRange());

        var session = LiveTemplatesManager.Instance.CreateHotspotSessionAtopExistingText(
          statement.GetSolution(), new TextRange(caretPosition.Value), textControl,
          LiveTemplatesManager.EscapeAction.LeaveTextAndCaret, new[] { typeSpot, nameSpot });

        session.Execute();
      }
    }
  }
}