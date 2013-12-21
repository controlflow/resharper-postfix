using JetBrains.Annotations;
using JetBrains.ReSharper.ControlFlow.PostfixCompletion.LookupItems;
using JetBrains.ReSharper.Feature.Services.Lookup;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.TextControl;

namespace JetBrains.ReSharper.ControlFlow.PostfixCompletion.Templates
{
  // todo: array creation (too hard to impl for now)
  // todo: nullable types creation? (what for?)

  [PostfixTemplate(
    templateName: "new",
    description: "Produces instantiation expression for type",
    example: "new SomeType()", WorksOnTypes = true)]
  public class ObjectCreationTypeTemplate : IPostfixTemplate
  {
    public ILookupItem CreateItems(PostfixTemplateContext context)
    {
      var expressionContext = context.InnerExpression;

      var typeElement = expressionContext.ReferencedElement as ITypeElement;
      if (typeElement == null) return null;

      var instantiable = TypeUtils.IsInstantiable(typeElement, expressionContext.Expression);
      if (instantiable != TypeInstantiability.NotInstantiable)
      {
        var hasCtorWithParams = (instantiable & TypeInstantiability.CtorWithParameters) != 0;
        return new NewItem(expressionContext, hasCtorWithParams);
      }

      return null;
    }

    private sealed class NewItem : ExpressionPostfixLookupItem<IObjectCreationExpression>
    {
      [NotNull] private readonly ILookupItemsOwner myLookupItemsOwner;
      private readonly bool myHasCtorWithParams;

      public NewItem([NotNull] PrefixExpressionContext context, bool hasCtorWithParams)
        : base("new", context)
      {
        myHasCtorWithParams = hasCtorWithParams;
        myLookupItemsOwner = context.Parent.ExecutionContext.LookupItemsOwner;
      }

      protected override IObjectCreationExpression CreateExpression(
        CSharpElementFactory factory, ICSharpExpression expression)
      {
        var template = string.Format("new {0}()", expression.GetText());
        return (IObjectCreationExpression) factory.CreateExpressionAsIs(template, false);
      }

      protected override void AfterComplete(
        ITextControl textControl, IObjectCreationExpression expression)
      {
        var caretNode = (myHasCtorWithParams ? expression.LPar : (ITreeNode) expression);
        var endOffset = caretNode.GetDocumentRange().TextRange.EndOffset;
        textControl.Caret.MoveTo(endOffset, CaretVisualPlacement.DontScrollIfVisible);

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