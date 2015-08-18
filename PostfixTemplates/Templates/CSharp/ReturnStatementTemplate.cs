using JetBrains.Annotations;
using JetBrains.ReSharper.PostfixTemplates.CodeCompletion;
using JetBrains.ReSharper.PostfixTemplates.Contexts.CSharp;
using JetBrains.ReSharper.PostfixTemplates.LookupItems;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Impl;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.TextControl;

namespace JetBrains.ReSharper.PostfixTemplates.Templates.CSharp
{
  [PostfixTemplate(
    templateName: "return",
    description: "Returns expression from current function",
    example: "return expr;")]
  public class ReturnStatementTemplate : IPostfixTemplate<CSharpPostfixTemplateContext>
  {
    public PostfixTemplateInfo TryCreateInfo(CSharpPostfixTemplateContext context)
    {
      var expressionContext = context.OuterExpression;
      if (expressionContext == null || !expressionContext.CanBeStatement) return null;

      if (context.IsPreciseMode && IsWorthShowingInPreciseMode(expressionContext)) return null;

      return new PostfixTemplateInfo("return", expressionContext);
    }

    private static bool IsWorthShowingInPreciseMode([NotNull] CSharpPostfixExpressionContext expressionContext)
    {
      var declaration = expressionContext.PostfixContext.ContainingFunction;
      if (declaration == null) return false;

      var function = declaration.DeclaredElement;
      if (function == null) return false;

      var returnType = GetMethodReturnValueType(function, declaration);
      if (returnType == null) return false;

      var conversionRule = expressionContext.Expression.GetTypeConversionRule();
      return expressionContext.ExpressionType.IsImplicitlyConvertibleTo(returnType, conversionRule);
    }

    [CanBeNull]
    private static IType GetMethodReturnValueType([NotNull] IParametersOwner function, [NotNull] ICSharpFunctionDeclaration declaration)
    {
      var returnType = function.ReturnType;
      if (returnType.IsVoid()) return null;

      if (declaration.IsIterator) return null;

      // todo: replace this shit with return type util from R#
      if (declaration.IsAsync)
      {
        if (returnType.IsTask()) return null;

        // unwrap return type from Task<T>
        var genericTask = returnType as IDeclaredType;
        if (genericTask.IsGenericTask())
        {
          var typeElement = genericTask.GetTypeElement();
          if (typeElement != null)
          {
            var typeParameters = typeElement.TypeParameters;
            if (typeParameters.Count == 1)
            {
              var typeParameter = typeParameters[0];
              return genericTask.GetSubstitution()[typeParameter];
            }
          }
        }
      }

      return returnType;
    }

    public PostfixTemplateBehavior CreateBehavior(PostfixTemplateInfo info)
    {
      return new CSharpPostfixReturnStatementBehavior(info);
    }

    private sealed class CSharpPostfixReturnStatementBehavior : CSharpStatementPostfixTemplateBehavior<IReturnStatement>
    {
      public CSharpPostfixReturnStatementBehavior([NotNull] PostfixTemplateInfo info) : base(info) { }

      protected override IReturnStatement CreateStatement(CSharpElementFactory factory, ICSharpExpression expression)
      {
        return (IReturnStatement) factory.CreateStatement("return $0;", expression);
      }

      protected override void AfterComplete(ITextControl textControl, IReturnStatement statement)
      {
        FormatStatementOnSemicolon(statement);
        base.AfterComplete(textControl, statement);
      }
    }
  }
}