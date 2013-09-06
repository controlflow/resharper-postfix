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

      if (!context.ForceMode)
      {
        var rule = exprContext.Expression.GetTypeConversionRule();
        var predefined = exprContext.Expression.GetPredefinedType();

        // 'Exception.throw' case
        var referencedType = exprContext.ReferencedType;
        if (referencedType != null)
        {
          if (!rule.IsImplicitlyConvertibleTo(referencedType, predefined.Exception)) return;

          var typeElement = referencedType.GetTypeElement().NotNull();
          if (TypeUtils.IsInstantiable(typeElement, exprContext.Expression) == 0) return;
        }
        else // 'new Exception().throw' case
        {
          if (!exprContext.Type.IsResolved) return;
          if (!rule.IsImplicitlyConvertibleTo(exprContext.Type, predefined.Exception)) return;
        }
      }

      if (exprContext.ReferencedType == null)
      {
        consumer.Add(new ThrowExpressionLookupItem(exprContext));
      }
      else
      {
        consumer.Add(new ThrowTypeLookupItem(
          exprContext, exprContext.ReferencedType, context.LookupItemsOwner));
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

      public ThrowTypeLookupItem(
        [NotNull] PrefixExpressionContext context,
        [NotNull] IDeclaredType exceptionType,
        [NotNull] ILookupItemsOwner lookupItemsOwner)
        : base("throw", context)
      {
        myExceptionType = exceptionType;
        myLookupItemsOwner = lookupItemsOwner;
      }

      protected override bool SuppressSemicolonSuffix { get { return true; } }
      protected override IThrowStatement CreateStatement(CSharpElementFactory factory)
      {
        return (IThrowStatement)factory.CreateStatement("throw new Expr();");
      }

      protected override void PlaceExpression(
        IThrowStatement statement, ICSharpExpression expression, CSharpElementFactory factory)
      {
        var creationExpression = (IObjectCreationExpression)statement.Exception;

        creationExpression.TypeReference.NotNull().BindTo(
          myExceptionType.GetTypeElement().NotNull(),
          myExceptionType.GetSubstitution().NotNull());
      }

      protected override void AfterComplete(
        ITextControl textControl, Suffix suffix, IThrowStatement statement, int? caretPosition)
      {
        var exception = (IObjectCreationExpression) statement.Exception;
        var endOffset = exception.LPar.GetDocumentRange().TextRange.EndOffset;

        base.AfterComplete(textControl, suffix, statement, endOffset);

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