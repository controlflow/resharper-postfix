using JetBrains.Annotations;
using JetBrains.ReSharper.Feature.Services.Lookup;
using JetBrains.ReSharper.PostfixTemplates.LookupItems;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Impl;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.PostfixTemplates.Templates
{
  [PostfixTemplate(
    templateName: "yield",
    description: "Returns expression/yields value from iterator",
    example: "return expr;")]
  public class YieldReturnStatementTemplate : IPostfixTemplate
  {
    public ILookupItem CreateItem(PostfixTemplateContext context)
    {
      var expressionContext = context.OuterExpression;
      if (expressionContext == null || !expressionContext.CanBeStatement) return null;

      if (context.IsAutoCompletion)
      {
        var declaration = context.ContainingFunction;
        if (declaration == null) return null;

        var function = declaration.DeclaredElement;
        if (function == null) return null;

        var returnType = function.ReturnType;
        if (returnType.IsVoid()) return null;

        if (!IsOrCanBecameIterator(declaration, returnType)) return null;

        var elementType = GetIteratorElementType(returnType, declaration);

        var conversionRule = expressionContext.Expression.GetTypeConversionRule();
        if (!expressionContext.ExpressionType.IsImplicitlyConvertibleTo(elementType, conversionRule))
          return null;
      }

      return new YieldItem(expressionContext);
    }

    private static bool IsOrCanBecameIterator([NotNull] ICSharpFunctionDeclaration declaration,
                                              [NotNull] IType returnType)
    {
      if (declaration.IsIterator) return true;
      if (declaration.IsAsync) return false;

      if (returnType.IsGenericIEnumerable() || returnType.IsIEnumerable())
      {
        var collector = new RecursiveElementCollector<IReturnStatement>();
        collector.ProcessElement(declaration);
        return (collector.GetResults().Count == 0);
      }

      return false;
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

    private sealed class YieldItem : StatementPostfixLookupItem<IYieldStatement>
    {
      public YieldItem([NotNull] PrefixExpressionContext context) : base("yield", context) { }

      protected override IYieldStatement CreateStatement(CSharpElementFactory factory,
                                                         ICSharpExpression expression)
      {
        return (IYieldStatement) factory.CreateStatement("yield return $0;", expression);
      }
    }
  }
}