using JetBrains.Annotations;
using JetBrains.Application.Settings;
using JetBrains.ReSharper.Feature.Services.Lookup;
using JetBrains.ReSharper.PostfixTemplates.LookupItems;
using JetBrains.ReSharper.PostfixTemplates.Settings;
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

      if (context.IsAutoCompletion)
      {
        var conversionRule = expression.GetTypeConversionRule();
        var predefined = expression.GetPredefinedType();

        // 'Exception.throw' case
        if (referencedType != null)
        {
          if (!conversionRule.IsImplicitlyConvertibleTo(referencedType, predefined.Exception))
            return null;
          if (TypeUtils.IsInstantiable(referencedType, expression) == 0)
            return null;
        }
        else
        {
          // 'new Exception().throw' case
          var expressionType = expressionContext.ExpressionType;
          if (!expressionType.IsResolved) return null;

          if (!expressionType.IsImplicitlyConvertibleTo(predefined.Exception, conversionRule))
            return null;
        }
      }

      if (referencedType == null)
      {
        return new ThrowValueItem(expressionContext);
      }

      var instantiable = TypeUtils.IsInstantiable(referencedType, expression);
      var hasParameters = (instantiable & TypeInstantiability.CtorWithParameters) != 0;

      return new ThrowByTypeItem(expressionContext, hasParameters);
    }

    private sealed class ThrowValueItem : StatementPostfixLookupItem<IThrowStatement>
    {
      public ThrowValueItem([NotNull] PrefixExpressionContext context) : base("throw", context) { }

      protected override IThrowStatement CreateStatement(CSharpElementFactory factory,
                                                         ICSharpExpression expression)
      {
        return (IThrowStatement) factory.CreateStatement("throw $0;", expression);
      }
    }

    private sealed class ThrowByTypeItem : StatementPostfixLookupItem<IThrowStatement>
    {
      [NotNull] private readonly ILookupItemsOwner myLookupItemsOwner;
      private readonly bool myHasParameters;

      public ThrowByTypeItem([NotNull] PrefixExpressionContext context, bool hasParameters)
        : base("throw", context)
      {
        var executionContext = context.PostfixContext.ExecutionContext;
        myLookupItemsOwner = executionContext.LookupItemsOwner;
        myHasParameters = hasParameters;
      }

      protected override IThrowStatement CreateStatement(CSharpElementFactory factory,
                                                         ICSharpExpression expression)
      {
        return (IThrowStatement) factory.CreateStatement("throw new $0();", expression.GetText());
      }

      protected override void AfterComplete(ITextControl textControl, IThrowStatement statement)
      {
        var expression = (IObjectCreationExpression) statement.Exception;
        var endOffset = myHasParameters
          ? expression.LPar.GetDocumentRange().TextRange.EndOffset
          : statement.GetDocumentRange().TextRange.EndOffset;

        textControl.Caret.MoveTo(endOffset, CaretVisualPlacement.DontScrollIfVisible);
        if (!myHasParameters) return;

        var parenthesisRange = expression.LPar.GetDocumentRange().SetEndTo(
          expression.RPar.GetDocumentRange().TextRange.EndOffset).TextRange;

        var settingsStore = statement.GetSettingsStore();
        if (settingsStore.GetValue(PostfixSettingsAccessor.InvokeParameterInfo))
        {
          LookupUtil.ShowParameterInfo(
            statement.GetSolution(), textControl, parenthesisRange, null, myLookupItemsOwner);
        }
      }
    }
  }
}