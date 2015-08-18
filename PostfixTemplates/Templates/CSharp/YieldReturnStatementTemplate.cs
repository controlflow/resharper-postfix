using JetBrains.Annotations;
using JetBrains.ReSharper.PostfixTemplates.CodeCompletion;
using JetBrains.ReSharper.PostfixTemplates.Contexts.CSharp;
using JetBrains.ReSharper.PostfixTemplates.LookupItems;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Impl;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.TextControl;

namespace JetBrains.ReSharper.PostfixTemplates.Templates.CSharp
{
  [PostfixTemplate(
    templateName: "yield",
    description: "Yields value from iterator method",
    example: "yield return expr;")]
  public class YieldReturnStatementTemplate : IPostfixTemplate<CSharpPostfixTemplateContext>
  {
    public PostfixTemplateInfo TryCreateInfo(CSharpPostfixTemplateContext context)
    {
      var expressionContext = context.OuterExpression;
      if (expressionContext == null) return null;

      if (!expressionContext.CanBeStatement) return null;

      if (context.IsPreciseMode && !IsWorthShowingInPreciseMode(expressionContext)) return null;

      return new PostfixTemplateInfo("yield", expressionContext);
    }

    private static bool IsWorthShowingInPreciseMode([NotNull] CSharpPostfixExpressionContext expressionContext)
    {
      var declaration = expressionContext.PostfixContext.ContainingFunction;
      if (declaration == null) return false;

      var function = declaration.DeclaredElement;
      if (function == null) return false;

      var returnType = function.ReturnType;
      if (returnType.IsVoid()) return false;

      if (!IsAlreadyOrCanBecameIterator(declaration, returnType)) return false;

      var elementType = GetIteratorElementType(returnType, declaration);
      // todo: allow if type is unknown?

      var conversionRule = expressionContext.Expression.GetTypeConversionRule();
      return expressionContext.ExpressionType.IsImplicitlyConvertibleTo(elementType, conversionRule);
    }

    private static bool IsAlreadyOrCanBecameIterator([NotNull] ICSharpFunctionDeclaration declaration, [NotNull] IType returnType)
    {
      if (declaration.IsIterator) return true;
      if (declaration.IsAsync) return false;

      if (returnType.IsGenericIEnumerable()
          || returnType.IsGenericIEnumerator()
          || returnType.IsIEnumerable()
          || returnType.IsIEnumerator())
      {
        return !declaration.Descendants<IReturnStatement>().Any();
      }

      return false;
    }

    public PostfixTemplateBehavior CreateBehavior(PostfixTemplateInfo info)
    {
      return new CSharpPostfixYieldStatementBehavior(info);
    }

    [NotNull]
    private static IType GetIteratorElementType([NotNull] IType returnType, [NotNull] ITreeNode context)
    {
      if (returnType.IsIEnumerable())
      {
        return context.GetPredefinedType().Object;
      }

      // unwrap return type from IEnumerable<T>
      var enumerable = returnType as IDeclaredType;
      // ReSharper disable once MergeSequentialChecks
      if (enumerable != null && enumerable.IsGenericIEnumerable())
      {
        var typeElement = enumerable.GetTypeElement();
        if (typeElement != null)
        {
          var typeParameters = typeElement.TypeParameters;
          if (typeParameters.Count == 1)
          {
            return enumerable.GetSubstitution()[typeParameters[0]];
          }
        }
      }

      return returnType;
    }

    private sealed class CSharpPostfixYieldStatementBehavior : CSharpStatementPostfixTemplateBehavior<IYieldStatement>
    {
      public CSharpPostfixYieldStatementBehavior([NotNull] PostfixTemplateInfo info) : base(info) { }

      protected override IYieldStatement CreateStatement(CSharpElementFactory factory, ICSharpExpression expression)
      {
        return (IYieldStatement) factory.CreateStatement("yield return $0;", expression);
      }

      protected override void AfterComplete(ITextControl textControl, IYieldStatement statement)
      {
        FormatStatementOnSemicolon(statement);
        base.AfterComplete(textControl, statement);
      }
    }
  }
}