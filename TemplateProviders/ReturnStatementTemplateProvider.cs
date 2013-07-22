using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.ReSharper.ControlFlow.PostfixCompletion.LookupItems;
using JetBrains.ReSharper.Feature.Services.Lookup;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Impl;
using JetBrains.ReSharper.Psi.CSharp.Tree;
#if RESHARPER8
using JetBrains.ReSharper.Psi.Modules;
#endif

namespace JetBrains.ReSharper.ControlFlow.PostfixCompletion.TemplateProviders
{
  [PostfixTemplateProvider(new []{"return", "yield"}, "Returns expression/yields value from iterator")]
  public class ReturnStatementTemplateProvider : IPostfixTemplateProvider
  {
    public void CreateItems(PostfixTemplateAcceptanceContext context, ICollection<ILookupItem> consumer)
    {
      var exprContext = context.PossibleExpressions.LastOrDefault();
      if (exprContext == null || !exprContext.CanBeStatement) return;

      var declaration = context.ContainingFunction;
      if (declaration == null) return;

      var function = declaration.DeclaredElement;
      if (function == null) return;

      if (context.ForceMode)
      {
        consumer.Add(new ReturnLookupItem(exprContext));
        consumer.Add(new YieldLookupItem(exprContext));
      }
      else // types check
      {
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
    }

    private static bool IsOrCanBecameIterator(
      [NotNull] ICSharpFunctionDeclaration declaration, [NotNull] IType returnType)
    {
      if (declaration.IsIterator) return true;

      if (returnType.IsGenericIEnumerable() || returnType.IsIEnumerable())
      {
        var collector = new RecursiveElementCollector<IReturnStatement>();
        collector.ProcessElement(declaration);
        return collector.GetResults().Count == 0;
      }

      return false;
    }

    private sealed class ReturnLookupItem : StatementPostfixLookupItem<IReturnStatement>
    {
      public ReturnLookupItem([NotNull] PrefixExpressionContext context)
        : base("return", context) { }

      protected override bool ShortcutIsCSharpStatementKeyword { get { return true; } }
      protected override bool SupressCommaSuffix { get { return true; } }

      protected override IReturnStatement CreateStatement(
        IPsiModule psiModule, CSharpElementFactory factory)
      {
        return (IReturnStatement) factory.CreateStatement("return expr;");
      }

      protected override void PlaceExpression(
        IReturnStatement statement, ICSharpExpression expression, CSharpElementFactory factory)
      {
        statement.Value.ReplaceBy(expression);
      }
    }

    private sealed class YieldLookupItem : StatementPostfixLookupItem<IYieldStatement>
    {
      public YieldLookupItem([NotNull] PrefixExpressionContext context)
        : base("yield", context) { }

      protected override bool ShortcutIsCSharpStatementKeyword { get { return true; } }
      protected override bool SupressCommaSuffix { get { return true; } }

      protected override IYieldStatement CreateStatement(
        IPsiModule psiModule, CSharpElementFactory factory)
      {
        return (IYieldStatement) factory.CreateStatement("yield return expr;");
      }

      protected override void PlaceExpression(
        IYieldStatement statement, ICSharpExpression expression, CSharpElementFactory factory)
      {
        statement.Expression.ReplaceBy(expression);
      }
    }
  }
}