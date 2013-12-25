using JetBrains.Annotations;
using JetBrains.ReSharper.Feature.Services.Lookup;
using JetBrains.ReSharper.PostfixTemplates.LookupItems;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Impl;
using JetBrains.ReSharper.Psi.CSharp.Tree;

namespace JetBrains.ReSharper.PostfixTemplates.Templates
{
  // TODO: null.return do not works?

  [PostfixTemplate(
    templateName: "return",
    description: "Returns expression/yields value from iterator",
    example: "return expr;")]
  public class ReturnStatementTemplate : IPostfixTemplate
  {
    public ILookupItem CreateItem(PostfixTemplateContext context)
    {
      var expressionContext = context.OuterExpression;
      if (!expressionContext.CanBeStatement) return null;

      var declaration = context.ContainingFunction;
      if (declaration == null) return null;

      var function = declaration.DeclaredElement;
      if (function == null) return null;

      if (!context.IsAutoCompletion)
      {
        return new ReturnLookupItem(expressionContext);
      }

      var returnType = function.ReturnType;
      if (returnType.IsVoid()) return null;

      if (!declaration.IsIterator)
      {
        if (declaration.IsAsync)
        {
          if (returnType.IsTask()) return null; // no return value

          // unwrap return type from Task<T>
          var genericTask = returnType as IDeclaredType;
          if (genericTask != null && genericTask.IsGenericTask())
          {
            var typeElement = genericTask.GetTypeElement();
            if (typeElement != null)
            {
              var typeParameters = typeElement.TypeParameters;
              if (typeParameters.Count == 1)
              {
                var typeParameter = typeParameters[0];
                returnType = genericTask.GetSubstitution()[typeParameter];
              }
            }
          }
        }

        var rule = expressionContext.Expression.GetTypeConversionRule();
        if (rule.IsImplicitlyConvertibleTo(expressionContext.Type, returnType))
        {
          return new ReturnLookupItem(expressionContext);
        }
      }

      return null;
    }

    private sealed class ReturnLookupItem : StatementPostfixLookupItem<IReturnStatement>
    {
      public ReturnLookupItem([NotNull] PrefixExpressionContext context)
        : base("return", context) { }

      protected override IReturnStatement CreateStatement(CSharpElementFactory factory,
                                                          ICSharpExpression expression)
      {
        return (IReturnStatement) factory.CreateStatement("return $0;", expression);
      }
    }
  }
}