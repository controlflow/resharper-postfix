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
  // todo: nullable types creation? (what for?)

  [PostfixTemplate(
    templateName: "new",
    description: "Produces instantiation expression for type",
    example: "new SomeType()", WorksOnTypes = true)]
  public class ObjectCreationTypeTemplate : IPostfixTemplate {
    public ILookupItem CreateItems(PostfixTemplateContext context) {
      var expressionContext = context.InnerExpression;

      var typeElement = expressionContext.ReferencedElement as ITypeElement;
      if (typeElement == null) return null;

      var instantiable = TypeUtils.IsInstantiable(typeElement, expressionContext.Expression);
      if (instantiable != TypeInstantiability.NotInstantiable) {
        var hasCtorWithParams = (instantiable & TypeInstantiability.CtorWithParameters) != 0;
        return new NewItem(expressionContext, context.LookupItemsOwner, hasCtorWithParams);
      }

      return null;
    }

    private sealed class NewItem : ExpressionPostfixLookupItem<IObjectCreationExpression>
    {
      [NotNull] private readonly string myTypeText;
      [NotNull] private readonly ILookupItemsOwner myLookupItemsOwner;
      private readonly bool myHasCtorWithParams;

      public NewItem([NotNull] PrefixExpressionContext context,
                     [NotNull] ILookupItemsOwner lookupItemsOwner,
                     bool hasCtorWithParams) : base("new", context) {
        myLookupItemsOwner = lookupItemsOwner;
        myHasCtorWithParams = hasCtorWithParams;
        myTypeText = context.Expression.GetText();
      }

      protected override IObjectCreationExpression CreateExpression(CSharpElementFactory factory,
                                                                    ICSharpExpression expression) {
        var format = myHasCtorWithParams ? "new {0}({1})" : "new {0}(){1}";
        var template = string.Format(format, myTypeText, CaretMarker);

        return (IObjectCreationExpression) factory.CreateExpressionAsIs(template, false);
      }

      protected override void AfterComplete(ITextControl textControl, Suffix suffix,
                                            IObjectCreationExpression expression, int? caretPosition) {
        if (caretPosition == null) {
          caretPosition = expression.GetDocumentRange().TextRange.EndOffset;
        }

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