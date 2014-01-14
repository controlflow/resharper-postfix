using JetBrains.Annotations;
using JetBrains.ReSharper.PostfixTemplates.LookupItems;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;

namespace JetBrains.ReSharper.PostfixTemplates.Templates
{
  public abstract class CheckForNullTemplateBase
  {
    [ContractAnnotation("null => false")]
    protected static bool IsNullable([CanBeNull] PrefixExpressionContext expressionContext)
    {
      if (expressionContext == null) return false;

      var expression = expressionContext.Expression;
      if (expression is IThisExpression) return false;
      if (expression is IBaseExpression) return false;
      if (expression is ICSharpLiteralExpression) return false;
      if (expression is IObjectCreationExpression) return false;
      if (expression is IUnaryOperatorExpression) return false;
      if (expression is INullCoalescingExpression) return true;
      if (expression is IBinaryExpression) return false;
      if (expression is IAnonymousMethodExpression) return false;
      if (expression is IAnonymousObjectCreationExpression) return false;
      if (expression is IArrayCreationExpression) return false;
      if (expression is IDefaultExpression) return false;
      if (expression is ITypeofExpression) return false;

      switch (expressionContext.Type.Classify)
      {
        case null:

        case TypeClassification.REFERENCE_TYPE:
          return true;

        case TypeClassification.VALUE_TYPE:
          return expressionContext.Type.IsNullable();

        default:
          return false;
      }
    }

    protected class CheckForNullStatementItem : StatementPostfixLookupItem<IIfStatement>
    {
      [NotNull] private readonly string myTemplate;

      public CheckForNullStatementItem([NotNull] string shortcut,
                                       [NotNull] PrefixExpressionContext context,
                                       [NotNull] string template)
        : base(shortcut, context)
      {
        myTemplate = template;
      }

      protected override IIfStatement CreateStatement(CSharpElementFactory factory,
                                                      ICSharpExpression expression)
      {
        var template = myTemplate + EmbeddedStatementBracesTemplate;
        return (IIfStatement) factory.CreateStatement(template, expression);
      }
    }

    protected class CheckForNullExpressionItem : ExpressionPostfixLookupItem<IEqualityExpression>
    {
      [NotNull] private readonly string myTemplate;

      public CheckForNullExpressionItem([NotNull] string shortcut,
                                        [NotNull] PrefixExpressionContext context,
                                        [NotNull] string template)
        : base(shortcut, context)
      {
        myTemplate = template;
      }

      protected override IEqualityExpression CreateExpression(CSharpElementFactory factory,
                                                              ICSharpExpression expression)
      {
        return (IEqualityExpression) factory.CreateExpression(myTemplate, expression);
      }
    }
  }
}