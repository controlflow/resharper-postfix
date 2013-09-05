using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.ReSharper.ControlFlow.PostfixCompletion.LookupItems;
using JetBrains.ReSharper.Feature.Services.Lookup;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.TextControl;

namespace JetBrains.ReSharper.ControlFlow.PostfixCompletion.TemplateProviders
{
  // todo: can create array? - too hard for now

  [PostfixTemplateProvider(
    templateName: "new",
    description: "Produces instantiation expression for type",
    example: "new SomeType()", WorksOnTypes = true)]
  public class ObjectCreationTypeTemplateProvider : IPostfixTemplateProvider
  {
    public void CreateItems(PostfixTemplateAcceptanceContext context, ICollection<ILookupItem> consumer)
    {
      var exprContext = context.InnerExpression;

      if (!CommonUtils.CanTypeBecameExpression(exprContext.Expression)) return;

      var typeElement = exprContext.ReferencedElement as ITypeElement;
      if (typeElement != null &&
        CommonUtils.IsInstantiable(typeElement, exprContext.Expression))
      {
        consumer.Add(new LookupItem(exprContext, context.LookupItemsOwner));
      }
    }

    private sealed class LookupItem : ExpressionPostfixLookupItem<IObjectCreationExpression>
    {
      [NotNull] private readonly string myTypeText;
      [NotNull] private readonly ILookupItemsOwner myLookupItemsOwner;

      public LookupItem(
        [NotNull] PrefixExpressionContext context, [NotNull] ILookupItemsOwner lookupItemsOwner)
        : base("new", context)
      {
        myLookupItemsOwner = lookupItemsOwner;
        myTypeText = context.Expression.GetText();
      }

      protected override IObjectCreationExpression CreateExpression(
        CSharpElementFactory factory, ICSharpExpression expression)
      {
        return (IObjectCreationExpression)
          factory.CreateExpression("new $0(" + CaretMarker + ")", myTypeText);
      }

      protected override void AfterComplete(
        ITextControl textControl, Suffix suffix,
        IObjectCreationExpression expression, int? caretPosition)
      {
        base.AfterComplete(textControl, suffix, expression, caretPosition);

        // todo: arrays?
        var parenthesisRange =
          expression.LPar.GetDocumentRange().SetEndTo(
          expression.RPar.GetDocumentRange().TextRange.EndOffset).TextRange;

        var solution = expression.GetSolution();
        LookupUtil.ShowParameterInfo(
          solution, textControl, parenthesisRange, null, myLookupItemsOwner);
      }
    }
  }
}