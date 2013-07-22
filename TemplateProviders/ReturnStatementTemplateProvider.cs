using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.ReSharper.ControlFlow.PostfixCompletion.LookupItems;
using JetBrains.ReSharper.Feature.Services.Lookup;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Impl;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Modules;

namespace JetBrains.ReSharper.ControlFlow.PostfixCompletion.TemplateProviders
{
  [PostfixTemplateProvider("return", "Returns expression")]
  public class ReturnStatementTemplateProvider : IPostfixTemplateProvider
  {
    public void CreateItems(PostfixTemplateAcceptanceContext context, ICollection<ILookupItem> consumer)
    {
      var exprContext = context.PossibleExpressions.LastOrDefault();
      if (exprContext == null) return;

      if (!context.CanBeStatement) return;
      if (!context.ForceMode && exprContext.Type.IsUnknown) return;

      var declaration = context.ContainingFunction;
      if (declaration != null && !declaration.IsAsync && !declaration.IsIterator)
      {
        var declaredElement = declaration.DeclaredElement;
        if (declaredElement != null)
        {
          var returnType = declaredElement.ReturnType;
          if (returnType.IsVoid()) return;
  
          if (!context.ForceMode)
          {
            var rule = context.MostInnerExpression.GetTypeConversionRule();
            if (!rule.IsImplicitlyConvertibleTo(exprContext.Type, returnType)) return;
          }
  
          consumer.Add(new LookupItem(exprContext));
        }
      }
  
      // todo: yield
    }

    private sealed class LookupItem : StatementPostfixLookupItem<IReturnStatement>
    {
      public LookupItem([NotNull] PrefixExpressionContext context)
        : base("return", context) { }

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
  }
}