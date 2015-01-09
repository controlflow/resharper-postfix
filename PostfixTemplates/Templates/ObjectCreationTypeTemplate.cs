using JetBrains.Annotations;
using JetBrains.Application.Settings;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.CodeCompletion;
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
    public IPostfixLookupItem CreateItem(PostfixTemplateContext context)
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
    private static IPostfixLookupItem CreateExpressionItem([NotNull] PostfixTemplateContext context)
    {
      var expressionContext = context.InnerExpression;
      if (expressionContext == null) return null;

      var invocationExpression = expressionContext.Expression as IInvocationExpression;
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
              {
                return new NewExpressionItem(expressionContext);
              }
            }
          }
          else if (declaredElement == null || declaredElement is ITypeElement)
          {
            if (CommonUtils.IsReferenceExpressionsChain(reference))
            {
              return new NewExpressionItem(expressionContext);
            }
          }
        }
      }
      else if (!context.IsAutoCompletion) // UnresolvedType.new
      {
        var reference = expressionContext.Expression as IReferenceExpression;
        if (reference != null && CommonUtils.IsReferenceExpressionsChain(reference))
        {
          var declaredElement = reference.Reference.Resolve().DeclaredElement;
          if (declaredElement == null || declaredElement is ITypeElement)
          {
            return new NewTypeItem(expressionContext, hasRequiredArguments: true);
          }
        }
      }

      return null;
    }

    private sealed class NewTypeItem : ExpressionPostfixLookupItem<IObjectCreationExpression>
    {
      private readonly bool myHasRequiredArguments;
      [NotNull] private readonly ILookupItemsOwner myLookupItemsOwner;

      public NewTypeItem([NotNull] PrefixExpressionContext context, bool hasRequiredArguments)
        : base("new", context)
      {
        myHasRequiredArguments = hasRequiredArguments;
        myLookupItemsOwner = context.PostfixContext.ExecutionContext.LookupItemsOwner;
      }

      protected override IObjectCreationExpression CreateExpression(CSharpElementFactory factory, ICSharpExpression expression)
      {
        var settingsStore = expression.GetSettingsStore();
        var parenthesesType = settingsStore.GetValue(CodeCompletionSettingsAccessor.ParenthesesInsertType);

        var template = string.Format("new {0}{1}", expression.GetText(), parenthesesType.GetParenthesesTemplate());
        return (IObjectCreationExpression) factory.CreateExpressionAsIs(template, false);
      }


      protected override void AfterComplete(ITextControl textControl, IObjectCreationExpression expression)
      {
        var settingsStore = expression.GetSettingsStore();

        var parenthesesType = settingsStore.GetValue(CodeCompletionSettingsAccessor.ParenthesesInsertType);
        if (parenthesesType == ParenthesesInsertType.None) return;

        var caretNode = myHasRequiredArguments ? expression.LPar : (ITreeNode) expression;
        var endOffset = caretNode.GetDocumentRange().TextRange.EndOffset;

        textControl.Caret.MoveTo(endOffset, CaretVisualPlacement.DontScrollIfVisible);

        if (myHasRequiredArguments && settingsStore.GetValue(PostfixSettingsAccessor.InvokeParameterInfo))
        {
          var solution = expression.GetSolution();
          LookupUtil.ShowParameterInfo(solution, textControl, myLookupItemsOwner);
        }
      }
    }

    private sealed class NewExpressionItem : ExpressionPostfixLookupItem<IObjectCreationExpression>
    {
      public NewExpressionItem([NotNull] PrefixExpressionContext context)
        : base("new", context) { }

      protected override IObjectCreationExpression CreateExpression(CSharpElementFactory factory, ICSharpExpression expression)
      {
        var template = string.Format("new {0}", expression.GetText());
        return (IObjectCreationExpression) factory.CreateExpressionAsIs(template, applyCodeFormatter: false);
      }
    }
  }
}