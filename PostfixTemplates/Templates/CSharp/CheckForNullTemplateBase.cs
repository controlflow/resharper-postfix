using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.ReSharper.PostfixTemplates.CodeCompletion;
using JetBrains.ReSharper.PostfixTemplates.Contexts;
using JetBrains.ReSharper.PostfixTemplates.Contexts.CSharp;
using JetBrains.ReSharper.PostfixTemplates.LookupItems;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.CSharp.Util;

namespace JetBrains.ReSharper.PostfixTemplates.Templates.CSharp
{
  // todo: check (new C()).notnull is not available
  // todo: public Ctor(string arg) { _arg = arg.notnull; } - disable
  // todo: parentheses!
  // todo: maybe enable in expression context

  public abstract class CheckForNullTemplateBase : IPostfixTemplate<CSharpPostfixTemplateContext>
  {
    PostfixTemplateInfo IPostfixTemplate<CSharpPostfixTemplateContext>.TryCreateInfo(CSharpPostfixTemplateContext context)
    {
      return TryCreateInfo(context);
    }

    [CanBeNull]
    protected abstract CheckForNullPostfixTemplateInfo TryCreateInfo(CSharpPostfixTemplateContext context);

    protected class CheckForNullPostfixTemplateInfo : PostfixTemplateInfo
    {
      public bool CheckNotNull { get; private set; }

      public CheckForNullPostfixTemplateInfo(
        [NotNull] string text, [NotNull] IEnumerable<PostfixExpressionContext> expressions, bool checkNotNull, PostfixTemplateTarget target)
        : base(text, expressions, target)
      {
        CheckNotNull = checkNotNull;
      }

      public CheckForNullPostfixTemplateInfo(
        [NotNull] string text, [NotNull] PostfixExpressionContext expression, bool checkNotNull, PostfixTemplateTarget target)
        : base(text, expression, target)
      {
        CheckNotNull = checkNotNull;
      }
    }

    public PostfixTemplateBehavior CreateBehavior(PostfixTemplateInfo info)
    {
      var checkForNullInfo = (CheckForNullPostfixTemplateInfo) info;
      if (checkForNullInfo.Target == PostfixTemplateTarget.Statement)
      {
        return new CheckForNullStatementItem(checkForNullInfo);
      }

      return new CheckForNullExpressionItem(checkForNullInfo);
    }

    [ContractAnnotation("null => false")]
    protected static bool IsNullable([CanBeNull] CSharpPostfixExpressionContext expressionContext)
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

    protected static bool MakeSenseToCheckInAuto(CSharpPostfixExpressionContext expressionContext)
    {
      var expression = expressionContext.Expression.GetOperandThroughParenthesis();
      if (expression is IAssignmentExpression) return false;

      // .notnull/.null over 'as T' expressions looks annoying
      if (expression is IAsExpression) return false;

      return true;
    }

    private sealed class CheckForNullStatementItem : CSharpStatementPostfixTemplateBehavior<IIfStatement>
    {
      [NotNull] private readonly string myTemplate;

      public CheckForNullStatementItem([NotNull] CheckForNullPostfixTemplateInfo info) : base(info)
      {
        myTemplate = info.CheckNotNull ? "if($0!=null)" : "if($0==null)";
      }

      protected override IIfStatement CreateStatement(CSharpElementFactory factory, ICSharpExpression expression)
      {
        var template = myTemplate + EmbeddedStatementBracesTemplate;
        return (IIfStatement) factory.CreateStatement(template, expression);
      }
    }

    private sealed class CheckForNullExpressionItem : CSharpExpressionPostfixTemplateBehavior<IEqualityExpression>
    {
      [NotNull] private readonly string myTemplate;

      public CheckForNullExpressionItem([NotNull] CheckForNullPostfixTemplateInfo info) : base(info)
      {
        myTemplate = info.CheckNotNull ? "$0!=null" : "$0==null";
      }

      protected override IEqualityExpression CreateExpression(CSharpElementFactory factory, ICSharpExpression expression)
      {
        return (IEqualityExpression) factory.CreateExpression(myTemplate, expression);
      }
    }
  }
}
