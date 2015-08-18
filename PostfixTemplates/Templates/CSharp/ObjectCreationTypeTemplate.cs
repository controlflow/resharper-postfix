using JetBrains.Annotations;
using JetBrains.Application.Settings;
using JetBrains.ReSharper.Feature.Services.CodeCompletion;
using JetBrains.ReSharper.Feature.Services.Lookup;
using JetBrains.ReSharper.PostfixTemplates.CodeCompletion;
using JetBrains.ReSharper.PostfixTemplates.Contexts.CSharp;
using JetBrains.ReSharper.PostfixTemplates.LookupItems;
using JetBrains.ReSharper.PostfixTemplates.Settings;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.TextControl;

namespace JetBrains.ReSharper.PostfixTemplates.Templates.CSharp
{
  [PostfixTemplate(
    templateName: "new",
    description: "Produces instantiation expression for type",
    example: "new SomeType()")]
  public class ObjectCreationTypeTemplate : IPostfixTemplate<CSharpPostfixTemplateContext>
  {
    [NotNull] private readonly LookupItemsOwnerFactory myLookupItemsOwnerFactory;

    public ObjectCreationTypeTemplate([NotNull] LookupItemsOwnerFactory lookupItemsOwnerFactory)
    {
      myLookupItemsOwnerFactory = lookupItemsOwnerFactory;
    }

    public PostfixTemplateInfo TryCreateInfo(CSharpPostfixTemplateContext context)
    {
      var typeExpression = context.TypeExpression;
      if (typeExpression == null)
      {
        return TryCreateExpressionInfo(context);
      }

      var typeElement = typeExpression.ReferencedElement as ITypeElement;
      if (typeElement == null) return null;

      if (context.IsPreciseMode)
      {
        if (!TypeUtils.IsUsefulToCreateWithNew(typeElement)) return null;
      }

      var canInstantiate = TypeUtils.CanInstantiateType(typeElement, typeExpression.Expression);
      if (canInstantiate != CanInstantiate.No)
      {
        return new PostfixTemplateInfo("new", typeExpression, PostfixTemplateTarget.TypeUsage);
      }

      return null;
    }

    [CanBeNull]
    private static PostfixTemplateInfo TryCreateExpressionInfo([NotNull] CSharpPostfixTemplateContext context)
    {
      var expressionContext = context.InnerExpression;
      if (expressionContext == null) return null;

      var invocationExpression = expressionContext.Expression as IInvocationExpression;
      if (invocationExpression != null) 
      {
        var reference = invocationExpression.InvokedExpression as IReferenceExpression;
        if (reference != null)
        {
          var resolveResult = reference.Reference.Resolve();
          var declaredElement = resolveResult.DeclaredElement;

          if (context.IsPreciseMode)
          {
            var typeElement = declaredElement as ITypeElement;
            if (typeElement != null && TypeUtils.IsUsefulToCreateWithNew(typeElement))
            {
              var canInstantiate = TypeUtils.CanInstantiateType(typeElement, reference);
              if (canInstantiate != CanInstantiate.No)
              {
                return new PostfixTemplateInfo("new", expressionContext);
              }
            }
          }
          else if (declaredElement == null || declaredElement is ITypeElement)
          {
            if (CommonUtils.IsReferenceExpressionsChain(reference))
            {
              return new PostfixTemplateInfo("new", expressionContext);
            }
          }
        }

        return null;
      }

      if (!context.IsPreciseMode) // UnresolvedType.new
      {
        var reference = expressionContext.Expression as IReferenceExpression;
        if (reference != null && CommonUtils.IsReferenceExpressionsChain(reference))
        {
          var resolveResult = reference.Reference.Resolve();

          var declaredElement = resolveResult.DeclaredElement;
          if (declaredElement == null || declaredElement is ITypeElement)
          {
            // hasRequiredArguments: true
            return new PostfixTemplateInfo("new", expressionContext, PostfixTemplateTarget.TypeUsage);
          }
        }
      }

      return null;
    }

    public PostfixTemplateBehavior CreateBehavior(PostfixTemplateInfo info)
    {
      if (info.Target == PostfixTemplateTarget.TypeUsage)
        return new CSharpPostfixObjectCreationTypeUsageBehavior(info, myLookupItemsOwnerFactory);

      return new CSharpPostfixObjectCreationExpressionBehavior(info);
    }

    private sealed class CSharpPostfixObjectCreationTypeUsageBehavior : CSharpExpressionPostfixTemplateBehavior<IObjectCreationExpression>
    {
      [NotNull] private readonly LookupItemsOwnerFactory myLookupItemsOwnerFactory;

      public CSharpPostfixObjectCreationTypeUsageBehavior(
        [NotNull] PostfixTemplateInfo info, [NotNull] LookupItemsOwnerFactory lookupItemsOwnerFactory) : base(info)
      {
        myLookupItemsOwnerFactory = lookupItemsOwnerFactory;
      }

      protected override IObjectCreationExpression CreateExpression(CSharpElementFactory factory, ICSharpExpression expression)
      {
        var settingsStore = expression.GetSettingsStore(); // todo: execution context?
        var parenthesesType = settingsStore.GetValue(CodeCompletionSettingsAccessor.ParenthesesInsertType);

        var template = string.Format("new {0}{1}", expression.GetText(), parenthesesType.GetParenthesesTemplate());
        return (IObjectCreationExpression) factory.CreateExpressionAsIs(template, false);
      }

      protected override void AfterComplete(ITextControl textControl, IObjectCreationExpression expression)
      {
        var settingsStore = expression.GetSettingsStore();

        var parenthesesType = settingsStore.GetValue(CodeCompletionSettingsAccessor.ParenthesesInsertType);
        if (parenthesesType == ParenthesesInsertType.None) return;

        var canInstantiate = TypeUtils.CanInstantiateType(expression.Type(), expression);
        var hasRequiredArguments = (canInstantiate & CanInstantiate.ConstructorWithParameters) != 0;

        var caretNode = hasRequiredArguments ? expression.LPar : (ITreeNode) expression;
        var endOffset = caretNode.GetDocumentRange().TextRange.EndOffset;

        textControl.Caret.MoveTo(endOffset, CaretVisualPlacement.DontScrollIfVisible);

        if (hasRequiredArguments && settingsStore.GetValue(PostfixSettingsAccessor.InvokeParameterInfo))
        {
          var solution = expression.GetSolution();
          var lookupItemsOwner = myLookupItemsOwnerFactory.CreateLookupItemsOwner(textControl);

          LookupUtil.ShowParameterInfo(solution, textControl, lookupItemsOwner);
        }
      }
    }

    private sealed class CSharpPostfixObjectCreationExpressionBehavior : CSharpExpressionPostfixTemplateBehavior<IObjectCreationExpression>
    {
      public CSharpPostfixObjectCreationExpressionBehavior([NotNull] PostfixTemplateInfo info) : base(info) { }

      protected override IObjectCreationExpression CreateExpression(CSharpElementFactory factory, ICSharpExpression expression)
      {
        // yes, simply reinterpret expression as constructor call
        var template = string.Format("new {0}", expression.GetText());
        return (IObjectCreationExpression) factory.CreateExpressionAsIs(template, applyCodeFormatter: false);
      }
    }
  }
}