using JetBrains.Annotations;
using JetBrains.ReSharper.Feature.Services.Lookup;
using JetBrains.ReSharper.PostfixTemplates.LookupItems;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Impl;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.TextControl;

namespace JetBrains.ReSharper.PostfixTemplates.Templates
{
  [PostfixTemplate(
    templateName: "throw",
    description: "Throws expression of 'Exception' type",
    example: "throw expr;", WorksOnTypes = true)]
  public class ThrowStatementTemplate : IPostfixTemplate
  {
    public ILookupItem CreateItem(PostfixTemplateContext context)
    {
      var expressionContext = context.OuterExpression;
      if (!expressionContext.CanBeStatement) return null;

      var referencedType = expressionContext.ReferencedType;
      var expression = expressionContext.Expression;

      if (!context.IsForceMode)
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
        return new ThrowValueItem(expressionContext);
      }

      var instantiable = TypeUtils.IsInstantiable(referencedType, expression);
      var hasCtorWithParams = (instantiable & TypeInstantiability.CtorWithParameters) != 0;

      return new ThrowByTypeItem(expressionContext, hasCtorWithParams);
    }

    private sealed class ThrowValueItem : StatementPostfixLookupItem<IThrowStatement>
    {
      public ThrowValueItem([NotNull] PrefixExpressionContext context) : base("throw", context) { }

      protected override IThrowStatement CreateStatement(
        CSharpElementFactory factory, ICSharpExpression expression)
      {
        return (IThrowStatement) factory.CreateStatement("throw $0;", expression);
      }
    }

    private sealed class ThrowByTypeItem : StatementPostfixLookupItem<IThrowStatement>
    {
      [NotNull] private readonly ILookupItemsOwner myLookupItemsOwner;
      private readonly bool myHasCtorWithParams;

      public ThrowByTypeItem([NotNull] PrefixExpressionContext context, bool hasCtorWithParams)
        : base("throw", context)
      {
        myLookupItemsOwner = context.Parent.ExecutionContext.LookupItemsOwner;
        myHasCtorWithParams = hasCtorWithParams;
      }

      protected override IThrowStatement CreateStatement(
        CSharpElementFactory factory, ICSharpExpression expression)
      {
        return (IThrowStatement) factory.CreateStatement("throw new $0();", expression.GetText());
      }

      protected override void AfterComplete(ITextControl textControl, IThrowStatement statement)
      {
        var exception = (IObjectCreationExpression) statement.Exception;
        var endOffset = myHasCtorWithParams
          ? exception.LPar.GetDocumentRange().TextRange.EndOffset
          : statement.GetDocumentRange().TextRange.EndOffset;

        textControl.Caret.MoveTo(endOffset, CaretVisualPlacement.DontScrollIfVisible);
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