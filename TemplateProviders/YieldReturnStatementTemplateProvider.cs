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
    templateName: "yield",
    description: "Returns expression/yields value from iterator",
    example: "return expr;")]
  public class YieldReturnStatementTemplateProvider : IPostfixTemplateProvider
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
        consumer.Add(new YieldLookupItem(exprContext));
        return;
      }

      var returnType = function.ReturnType;
      if (returnType.IsVoid()) return;

      if (IsOrCanBecameIterator(declaration, returnType))
      {
        if (returnType.IsIEnumerable())
        {
          returnType = declaration.GetPredefinedType().Object;
        }
        else
        {
          // unwrap return type from IEnumerable<T>
          var enumerable = returnType as IDeclaredType;
          if (enumerable != null && enumerable.IsGenericIEnumerable())
          {
            var element = enumerable.GetTypeElement();
            if (element != null)
            {
              var typeParameters = element.TypeParameters;
              if (typeParameters.Count == 1)
                returnType = enumerable.GetSubstitution()[typeParameters[0]];
            }
          }
        }

        var rule = exprContext.Expression.GetTypeConversionRule();
        if (rule.IsImplicitlyConvertibleTo(exprContext.Type, returnType))
        {
          consumer.Add(new YieldLookupItem(exprContext));
        }
      }
    }

    private static bool IsOrCanBecameIterator(
      [NotNull] ICSharpFunctionDeclaration declaration, [NotNull] IType returnType)
    {
      if (declaration.IsIterator) return true;
      if (declaration.IsAsync) return false;

      if (returnType.IsGenericIEnumerable() || returnType.IsIEnumerable())
      {
        var collector = new RecursiveElementCollector<IReturnStatement>();
        collector.ProcessElement(declaration);
        return collector.GetResults().Count == 0;
      }

      return false;
    }

    private sealed class YieldLookupItem : StatementPostfixLookupItem<IYieldStatement>
    {
      public YieldLookupItem([NotNull] PrefixExpressionContext context)
        : base("yield", context) { }

      protected override bool SuppressSemicolonSuffix { get { return true; } }

      protected override IYieldStatement CreateStatement(CSharpElementFactory factory)
      {
        return (IYieldStatement)
          factory.CreateStatement("yield return expr;");
      }

      protected override void PlaceExpression(
        IYieldStatement statement, ICSharpExpression expression, CSharpElementFactory factory)
      {
        statement.Expression.ReplaceBy(expression);
      }
    }
  }
}