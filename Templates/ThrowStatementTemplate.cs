using JetBrains.Annotations;
using JetBrains.ReSharper.ControlFlow.PostfixCompletion.LookupItems;
using JetBrains.ReSharper.Feature.Services.Lookup;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Impl;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.TextControl;
using JetBrains.Util;

namespace JetBrains.ReSharper.ControlFlow.PostfixCompletion.Templates
{
  [PostfixTemplate(
    templateName: "throw",
    description: "Throws expression of 'Exception' type",
    example: "throw expr;", WorksOnTypes = true)]
  public class ThrowStatementTemplate : IPostfixTemplate
  {
    public ILookupItem CreateItems(PostfixTemplateContext context)
    {
      var expressionContext = context.OuterExpression;
      if (!expressionContext.CanBeStatement) return null;

      var referencedType = expressionContext.ReferencedType;
      var expression = expressionContext.Expression;

      if (!context.ForceMode)
      {
        var rule = expression.GetTypeConversionRule();
        var predefined = expression.GetPredefinedType();

        // 'Exception.throw' case
        if (referencedType != null)
        {
          if (!rule.IsImplicitlyConvertibleTo(referencedType, predefined.Exception)) return null;
          if (TypeUtils.IsInstantiable(referencedType, expression) == 0) return null;
        }
        else
        {
          // 'new Exception().throw' case
          if (!expressionContext.Type.IsResolved) return null;
          if (!rule.IsImplicitlyConvertibleTo(expressionContext.Type, predefined.Exception)) return null;
        }
      }

      if (referencedType == null)
      {
        return new ThrowExpressionLookupItem(expressionContext);
      }

      var instantiable = TypeUtils.IsInstantiable(referencedType, expression);
      var hasCtorWithParams = (instantiable & TypeInstantiability.CtorWithParameters) != 0;

      return new ThrowTypeLookupItem(
        expressionContext, referencedType, context.LookupItemsOwner, hasCtorWithParams);
    }

    private sealed class ThrowExpressionLookupItem : StatementPostfixLookupItem<IThrowStatement>
    {
      public ThrowExpressionLookupItem([NotNull] PrefixExpressionContext context) : base("throw", context) { }

      protected override bool SuppressSemicolonSuffix { get { return true; } }

      protected override IThrowStatement CreateStatement(CSharpElementFactory factory)
      {
        return (IThrowStatement) factory.CreateStatement("throw expr;");
      }

      protected override void PlaceExpression(
        IThrowStatement statement, ICSharpExpression expression, CSharpElementFactory factory)
      {
        statement.Exception.ReplaceBy(expression);
      }

      protected override void AfterComplete(
        ITextControl textControl, Suffix suffix, IThrowStatement statement, int? caretPosition)
      {
        if (caretPosition == null)
        {
          caretPosition = statement.GetDocumentRange().TextRange.EndOffset;
        }

        base.AfterComplete(textControl, suffix, statement, caretPosition);
      }
    }

    private sealed class ThrowTypeLookupItem : StatementPostfixLookupItem<IThrowStatement>
    {
      [NotNull] private readonly IDeclaredType myExceptionType;
      [NotNull] private readonly ILookupItemsOwner myLookupItemsOwner;
      private readonly bool myHasCtorWithParams;

      public ThrowTypeLookupItem([NotNull] PrefixExpressionContext context,
        [NotNull] IDeclaredType exceptionType,
        [NotNull] ILookupItemsOwner lookupItemsOwner,
        bool hasCtorWithParams)
        : base("throw", context)
      {
        myExceptionType = exceptionType;
        myLookupItemsOwner = lookupItemsOwner;
        myHasCtorWithParams = hasCtorWithParams;
      }

      protected override bool SuppressSemicolonSuffix
      {
        get { return true; }
      }

      protected override IThrowStatement CreateStatement(CSharpElementFactory factory)
      {
        return (IThrowStatement) factory.CreateStatement("throw new Expr();");
      }

      protected override void PlaceExpression(
        IThrowStatement statement, ICSharpExpression expression, CSharpElementFactory factory)
      {
        var creationExpression = (IObjectCreationExpression) statement.Exception;
        var typeReference = creationExpression.TypeReference;
        if (typeReference != null)
        {
          typeReference.BindTo(
            myExceptionType.GetTypeElement().NotNull(),
            myExceptionType.GetSubstitution().NotNull());
        }
      }

      protected override void AfterComplete(
        ITextControl textControl, Suffix suffix, IThrowStatement statement, int? caretPosition)
      {
        var exception = (IObjectCreationExpression) statement.Exception;
        var endOffset = myHasCtorWithParams
          ? exception.LPar.GetDocumentRange().TextRange.EndOffset
          : statement.GetDocumentRange().TextRange.EndOffset;

        base.AfterComplete(textControl, suffix, statement, endOffset);
        if (!myHasCtorWithParams) return;

        var parenthesisRange =
          exception.LPar.GetDocumentRange().SetEndTo(
            exception.RPar.GetDocumentRange().TextRange.EndOffset).TextRange;

        var solution = statement.GetSolution();
        LookupUtil.ShowParameterInfo(
          solution, textControl, parenthesisRange, null, myLookupItemsOwner);
      }
    }
  }
}