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
  // todo: array creation (too hard to impl for now)
  // todo: nullable types creation?

  [PostfixTemplateProvider(
    templateName: "new",
    description: "Produces instantiation expression for type",
    example: "new SomeType()", WorksOnTypes = true)]
  public class ObjectCreationTypeTemplateProvider : IPostfixTemplateProvider
  {
    public void CreateItems(PostfixTemplateAcceptanceContext context, ICollection<ILookupItem> consumer)
    {
      var exprContext = context.InnerExpression;

      var typeElement = exprContext.ReferencedElement as ITypeElement;
      if (typeElement == null) return;

      var instantiable = TypeUtils.IsInstantiable(typeElement, exprContext.Expression);
      if (instantiable != TypeInstantiability.NotInstantiable)
      {
        var hasCtorWithParams = (instantiable & TypeInstantiability.CtorWithParameters) != 0;
        consumer.Add(new LookupItem(exprContext, context.LookupItemsOwner, hasCtorWithParams));
      }
    }

    private sealed class LookupItem : ExpressionPostfixLookupItem<IObjectCreationExpression>
    {
      [NotNull] private readonly string myTypeText;
      [NotNull] private readonly ILookupItemsOwner myLookupItemsOwner;
      private readonly bool myHasCtorWithParams;

      public LookupItem(
        [NotNull] PrefixExpressionContext context,
        [NotNull] ILookupItemsOwner lookupItemsOwner,
        bool hasCtorWithParams) : base("new", context)
      {
        myLookupItemsOwner = lookupItemsOwner;
        myHasCtorWithParams = hasCtorWithParams;
        myTypeText = context.Expression.GetText();
      }

      protected override IObjectCreationExpression CreateExpression(
        CSharpElementFactory factory, ICSharpExpression expression)
      {
        var template = string.Format(
          myHasCtorWithParams ? "new {0}({1})" : "new {0}(){1}",
          myTypeText, CaretMarker);

        return (IObjectCreationExpression) factory.CreateExpressionAsIs(template, false);
      }

      protected override void AfterComplete(
        ITextControl textControl, Suffix suffix,
        IObjectCreationExpression expression, int? caretPosition)
      {
        base.AfterComplete(textControl, suffix, expression, caretPosition);
        if (!myHasCtorWithParams) return;

        var parenthesisRange =
          expression.LPar.GetDocumentRange().TextRange.SetEndTo(
            expression.RPar.GetDocumentRange().TextRange.EndOffset);

        var solution = expression.GetSolution();
        LookupUtil.ShowParameterInfo(
          solution, textControl, parenthesisRange, null, myLookupItemsOwner);
      }
    }
  }
}