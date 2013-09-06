using System.Collections.Generic;
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
#if RESHARPER7
using JetBrains.ReSharper.Psi;
#endif

namespace JetBrains.ReSharper.ControlFlow.PostfixCompletion.TemplateProviders
{
  [PostfixTemplateProvider(
    templateName: "throw",
    description: "Throws expression of 'Exception' type",
    example: "throw expr;", WorksOnTypes = true)]
  public class ThrowStatementTemplateProvider : IPostfixTemplateProvider
  {
    public void CreateItems(
      PostfixTemplateAcceptanceContext context, ICollection<ILookupItem> consumer)
    {
      var exprContext = context.OuterExpression;
      if (!exprContext.CanBeStatement) return;

      var referencedType = exprContext.ReferencedType;
      var expression = exprContext.Expression;

      if (!context.ForceMode)
      {
        var rule = expression.GetTypeConversionRule();
        var predefined = expression.GetPredefinedType();

        // 'Exception.throw' case
        if (referencedType != null)
        {
          if (!rule.IsImplicitlyConvertibleTo(referencedType, predefined.Exception)) return;
          if (TypeUtils.IsInstantiable(referencedType, expression) == 0) return;
        }
        else // 'new Exception().throw' case
        {
          if (!exprContext.Type.IsResolved) return;
          if (!rule.IsImplicitlyConvertibleTo(exprContext.Type, predefined.Exception)) return;
        }
      }

      if (referencedType == null)
      {
        consumer.Add(new ThrowExpressionLookupItem(exprContext));
      }
      else
      {
        var instantiable = TypeUtils.IsInstantiable(referencedType, expression);
        var hasCtorWithParams = (instantiable & TypeInstantiability.CtorWithParameters) != 0;

        consumer.Add(new ThrowTypeLookupItem(
          exprContext, referencedType, context.LookupItemsOwner, hasCtorWithParams));
      }
    }

    private sealed class ThrowExpressionLookupItem : StatementPostfixLookupItem<IThrowStatement>
    {
      public ThrowExpressionLookupItem([NotNull] PrefixExpressionContext context)
        : base("throw", context) { }

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
    }

    private sealed class ThrowTypeLookupItem : StatementPostfixLookupItem<IThrowStatement>
    {
      [NotNull] private readonly IDeclaredType myExceptionType;
      [NotNull] private readonly ILookupItemsOwner myLookupItemsOwner;
      private readonly bool myHasCtorWithParams;

      public ThrowTypeLookupItem(
        [NotNull] PrefixExpressionContext context,
        [NotNull] IDeclaredType exceptionType,
        [NotNull] ILookupItemsOwner lookupItemsOwner,
        bool hasCtorWithParams) : base("throw", context)
      {
        myExceptionType = exceptionType;
        myLookupItemsOwner = lookupItemsOwner;
        myHasCtorWithParams = hasCtorWithParams;
      }

      protected override bool SuppressSemicolonSuffix { get { return true; } }
      protected override IThrowStatement CreateStatement(CSharpElementFactory factory)
      {
        return (IThrowStatement)factory.CreateStatement("throw new Expr();");
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