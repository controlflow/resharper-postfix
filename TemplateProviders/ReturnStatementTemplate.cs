using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.ReSharper.ControlFlow.PostfixCompletion.LookupItems;
using JetBrains.ReSharper.Feature.Services.Lookup;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Impl;
using JetBrains.ReSharper.Psi.CSharp.Tree;

namespace JetBrains.ReSharper.ControlFlow.PostfixCompletion.TemplateProviders
{
  [PostfixTemplateProvider(
    templateName: "return",
    description: "Returns expression/yields value from iterator",
    example: "return expr;")]
  public class ReturnStatementTemplate : IPostfixTemplate
  {
    public void CreateItems(PostfixTemplateAcceptanceContext context, ICollection<ILookupItem> consumer)
    {
      var exprContext = context.OuterExpression;
      if (!exprContext.CanBeStatement) return;

      var declaration = context.ContainingFunction;
      if (declaration == null) return;

      var function = declaration.DeclaredElement;
      if (function == null) return;

      if (context.ForceMode)
      {
        consumer.Add(new ReturnLookupItem(exprContext));
        return;
      }

      var returnType = function.ReturnType;
      if (returnType.IsVoid()) return;

      if (!declaration.IsIterator)
      {
        if (declaration.IsAsync)
        {
          if (returnType.IsTask()) return; // no return value

          // unwrap return type from Task<T>
          var genericTask = returnType as IDeclaredType;
          if (genericTask != null && genericTask.IsGenericTask())
          {
            var element = genericTask.GetTypeElement();
            if (element != null)
            {
              var typeParameters = element.TypeParameters;
              if (typeParameters.Count == 1)
                returnType = genericTask.GetSubstitution()[typeParameters[0]];
            }
          }
        }

        var rule = exprContext.Expression.GetTypeConversionRule();
        if (rule.IsImplicitlyConvertibleTo(exprContext.Type, returnType))
        {
          consumer.Add(new ReturnLookupItem(exprContext));
        }
      }
    }

    private sealed class ReturnLookupItem : StatementPostfixLookupItem<IReturnStatement>
    {
      public ReturnLookupItem([NotNull] PrefixExpressionContext context)
        : base("return", context) { }

      protected override bool SuppressSemicolonSuffix { get { return true; } }

      protected override IReturnStatement CreateStatement(CSharpElementFactory factory)
      {
        return (IReturnStatement) factory.CreateStatement("return expr;");
      }

      protected override void PlaceExpression(
        IReturnStatement statement, ICSharpExpression expression, CSharpElementFactory factory)
      {
        statement.Value.ReplaceBy(expression);
      }
    }
  }
}