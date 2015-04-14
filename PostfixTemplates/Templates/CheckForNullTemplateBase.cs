using JetBrains.Annotations;
using JetBrains.ReSharper.PostfixTemplates.LookupItems;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.CSharp.Util;

namespace JetBrains.ReSharper.PostfixTemplates.Templates
{
  // todo: check (new C()).notnull is not available
  // todo: public Ctor(string arg) { _arg = arg.notnull; } - disable
  // todo: parentheses!
  // todo: maybe enable in expression context

  public abstract class CheckForNullTemplateBase
  {
    [ContractAnnotation("null => false")]
    protected static bool IsNullable([CanBeNull] PrefixExpressionContext expressionContext)
    {
      if (expressionContext == null) return false;

      var expression = expressionContext.Expression;
      if (expression is INullCoalescingExpression) return true;

      if (expression is IThisExpression
        || expression is IBaseExpression
        || expression is ICSharpLiteralExpression
        || expression is IObjectCreationExpression
        || expression is IUnaryOperatorExpression
        || expression is IBinaryExpression
        || expression is IAnonymousMethodExpression
        || expression is IAnonymousObjectCreationExpression
        || expression is IArrayCreationExpression
        || expression is IDefaultExpression
        || expression is ITypeofExpression) return false;

      var typeClassification = expressionContext.Type.Classify;
      if (typeClassification == TypeClassification.VALUE_TYPE)
      {
        return expressionContext.Type.IsNullable();
      }

      return true; // unknown or ref-type
    }

    protected static bool MakeSenseToCheckInAuto(PrefixExpressionContext expressionContext)
    {
      var expression = expressionContext.Expression.GetOperandThroughParenthesis();
      if (expression is IAssignmentExpression) return false;

      // .notnull/.null over 'as T' expressions looks annoying
      if (expression is IAsExpression) return false;

      return true;
    }

    protected class CheckForNullStatementItem : StatementPostfixLookupItem<IIfStatement>
    {
      [NotNull] private readonly string myTemplate;

      public CheckForNullStatementItem([NotNull] string shortcut, [NotNull] PrefixExpressionContext context, [NotNull] string template)
        : base(shortcut, context)
      {
        myTemplate = template;
      }

      protected override IIfStatement CreateStatement(CSharpElementFactory factory, ICSharpExpression expression)
      {
        var template = myTemplate + EmbeddedStatementBracesTemplate;
        return (IIfStatement) factory.CreateStatement(template, expression);
      }
    }

    protected class CheckForNullExpressionItem : ExpressionPostfixLookupItem<IEqualityExpression>
    {
      [NotNull] private readonly string myTemplate;

      public CheckForNullExpressionItem([NotNull] string shortcut, [NotNull] PrefixExpressionContext[] context, [NotNull] string template)
        : base(shortcut, context)
      {
        myTemplate = template;
      }

      protected override IEqualityExpression CreateExpression(CSharpElementFactory factory, ICSharpExpression expression)
      {
        return (IEqualityExpression) factory.CreateExpression(myTemplate, expression);
      }
    }
  }
}
