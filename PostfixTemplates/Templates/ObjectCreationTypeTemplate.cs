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
  [PostfixTemplate(
    templateName: "new",
    description: "Produces instantiation expression for type",
    example: "new SomeType()")]
  public class ObjectCreationTypeTemplate : IPostfixTemplate
  {
    public ILookupItem CreateItem(PostfixTemplateContext context)
    {
      var typeExpression = context.TypeExpression;
      if (typeExpression == null)
      {
        return CreateExpressionItem(context);
      }

      var typeElement = typeExpression.ReferencedElement as ITypeElement;
      if (typeElement == null) return null;

      if (context.IsAutoCompletion)
      {
        if (!TypeUtils.IsUsefulToCreateWithNew(typeElement)) return null;
      }

      var canInstantiate = TypeUtils.CanInstantiateType(typeElement, typeExpression.Expression);
      if (canInstantiate != CanInstantiate.No)
      {
        var hasParameters = (canInstantiate & CanInstantiate.ConstructorWithParameters) != 0;
        return new NewTypeItem(typeExpression, hasParameters);
      }

      return null;
    }

    [CanBeNull]
    private static ILookupItem CreateExpressionItem([NotNull] PostfixTemplateContext context)
    {
      var expressionContext = context.InnerExpression;
      if (expressionContext == null) return null;

      var expression = expressionContext.Expression;

      var invocationExpression = expression as IInvocationExpression;
      if (invocationExpression != null) // StringBuilder().new
      {
        var reference = invocationExpression.InvokedExpression as IReferenceExpression;
        if (reference != null)
        {
          var declaredElement = reference.Reference.Resolve().DeclaredElement;

          if (context.IsAutoCompletion)
          {
            var typeElement = declaredElement as ITypeElement;
            if (typeElement != null && TypeUtils.IsUsefulToCreateWithNew(typeElement))
            {
              var canInstantiate = TypeUtils.CanInstantiateType(typeElement, reference);
              if (canInstantiate != CanInstantiate.No)
                return new NewExpressionItem(expressionContext);
            }
          }
          else if (declaredElement == null || declaredElement is ITypeElement)
          {
            if (IsReferenceExpressionsChain(reference))
              return new NewExpressionItem(expressionContext);
          }
        }
      }
      else // UnresolvedType.new
      {
        var reference = expression as IReferenceExpression;
        if (reference != null && !context.IsAutoCompletion && IsReferenceExpressionsChain(reference))
        {
          var declaredElement = reference.Reference.Resolve().DeclaredElement;
          if (declaredElement == null || declaredElement is ITypeElement)
            return new NewTypeItem(expressionContext, hasParameters: true);
        }
      }

      return null;
    }

    private static bool IsReferenceExpressionsChain([CanBeNull] ICSharpExpression expression)
    {
      do
      {
        var referenceExpression = expression as IReferenceExpression;
        if (referenceExpression == null) return false;

        expression = referenceExpression.QualifierExpression;
      }
      while (expression != null);

      return true;
    }

    private sealed class NewTypeItem : ExpressionPostfixLookupItem<IObjectCreationExpression>
    {
      private readonly bool myHasParameters;
      [NotNull] private readonly ILookupItemsOwner myLookupItemsOwner;

      public NewTypeItem([NotNull] PrefixExpressionContext context, bool hasParameters)
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

    private sealed class NewExpressionItem : ExpressionPostfixLookupItem<IObjectCreationExpression>
    {
      public NewExpressionItem([NotNull] PrefixExpressionContext context)
        : base("new", context) { }

      protected override IObjectCreationExpression CreateExpression(CSharpElementFactory factory,
                                                                    ICSharpExpression expression)
      {
        var template = string.Format("new {0}", expression.GetText());
        return (IObjectCreationExpression) factory.CreateExpressionAsIs(template, false);
      }
    }
  }
}