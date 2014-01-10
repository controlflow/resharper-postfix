using System.Linq;
using JetBrains.Annotations;
using JetBrains.Application.Settings;
using JetBrains.ReSharper.Feature.Services.Lookup;
using JetBrains.ReSharper.PostfixTemplates.LookupItems;
using JetBrains.ReSharper.PostfixTemplates.Settings;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.TextControl;

namespace JetBrains.ReSharper.PostfixTemplates.Templates
{
  // todo: array creation (too hard to impl for now)
  // todo: nullable types creation? (what for?)

  [PostfixTemplate(
    templateName: "new",
    description: "Produces instantiation expression for type",
    example: "new SomeType()")]
  public class ObjectCreationTypeTemplate : IPostfixTemplate
  {
    public ILookupItem CreateItem(PostfixTemplateContext context)
    {
      var expressionContext = context.ExpressionsOrTypes[0];

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
      private readonly bool myHasParameters;
      [NotNull] private readonly ILookupItemsOwner myLookupItemsOwner;

      public NewItem([NotNull] PrefixExpressionContext context, bool hasParameters)
        : base("new", context)
      {
        myHasParameters = hasParameters;
        myLookupItemsOwner = context.PostfixContext.ExecutionContext.LookupItemsOwner;
      }

      protected override IObjectCreationExpression CreateExpression(CSharpElementFactory factory,
                                                                    ICSharpExpression expression)
      {
        var template = string.Format("new {0}()", expression.GetText());
        return (IObjectCreationExpression) factory.CreateExpressionAsIs(template, false);
      }

      protected override void AfterComplete(ITextControl textControl, IObjectCreationExpression expression)
      {
        var caretNode = (myHasParameters ? expression.LPar : (ITreeNode) expression);
        var endOffset = caretNode.GetDocumentRange().TextRange.EndOffset;
        textControl.Caret.MoveTo(endOffset, CaretVisualPlacement.DontScrollIfVisible);

        if (!myHasParameters) return;

        var parenthesisRange =
          expression.LPar.GetDocumentRange().TextRange.SetEndTo(
            expression.RPar.GetDocumentRange().TextRange.EndOffset);

        var settingsStore = expression.GetSettingsStore();
        if (settingsStore.GetValue(PostfixSettingsAccessor.InvokeParameterInfo))
        {
          LookupUtil.ShowParameterInfo(
            expression.GetSolution(), textControl, parenthesisRange, null, myLookupItemsOwner);
        }
      }
    }
  }
}