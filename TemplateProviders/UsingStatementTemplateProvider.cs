using System.Collections.Generic;
using JetBrains.ReSharper.ControlFlow.PostfixCompletion.LookupItems;
using JetBrains.ReSharper.Feature.Services.Lookup;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.ReSharper.Psi.Xaml.Impl;

namespace JetBrains.ReSharper.ControlFlow.PostfixCompletion.TemplateProviders
{
  [PostfixTemplateProvider("using", "Wrap resource with using statement")]
  public class UsingStatementTemplateProvider : IPostfixTemplateProvider
  {
    // todo: available here: var alreadyHasVar = smth.using

    public void CreateItems(PostfixTemplateAcceptanceContext context, ICollection<ILookupItem> consumer)
    {
      if (!context.CanBeStatement) return; // check declaration

      var predefined = context.Expression.GetPsiModule().GetPredefinedType();
      var rule = context.Expression.GetTypeConversionRule();
      if (!rule.IsImplicitlyConvertibleTo(context.ExpressionType, predefined.IDisposable))
        return;

      // check expression is local variable reference
      ILocalVariable usingVar = null;
      var expr = context.Expression as IReferenceExpression;
      if (expr != null && expr.QualifierExpression == null)
        usingVar = expr.Reference.Resolve().DeclaredElement as ILocalVariable;

      ITreeNode node = context.Expression;
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
            if (MiscUtil.AreExpressionsEquivalent(e, context.Expression))
              return;

        node = usingStatement;
      }

      consumer.Add(new NameSuggestionPostfixLookupItem(
        context, "using", "using (var $NAME$ = $EXPR$) $CARET$", context.Expression));
    }
  }
}