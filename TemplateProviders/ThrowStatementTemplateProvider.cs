using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.ReSharper.ControlFlow.PostfixCompletion.LookupItems;
using JetBrains.ReSharper.Feature.Services.Lookup;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Impl;
using JetBrains.ReSharper.Psi.CSharp.Tree;
#if RESHARPER7
using JetBrains.ReSharper.Psi;
#endif

namespace JetBrains.ReSharper.ControlFlow.PostfixCompletion.TemplateProviders
{
  // todo: make it works on types!

  [PostfixTemplateProvider(
    templateName: "throw",
    description: "Throws expression of 'Exception' type",
    example: "throw expr;")]
  public class ThrowStatementTemplateProvider : IPostfixTemplateProvider
  {
    public void CreateItems(PostfixTemplateAcceptanceContext context, ICollection<ILookupItem> consumer)
    {
      var exprContext = context.OuterExpression;
      if (!exprContext.CanBeStatement) return;

      if (!context.ForceMode)
      {
        var rule = exprContext.Expression.GetTypeConversionRule();
        var predefinedType = exprContext.Expression.GetPredefinedType();
        if (!rule.IsImplicitlyConvertibleTo(exprContext.Type, predefinedType.Exception))
          return;
      }

      consumer.Add(new LookupItem(exprContext));
    }

    private sealed class LookupItem : StatementPostfixLookupItem<IThrowStatement>
    {
      public LookupItem([NotNull] PrefixExpressionContext context)
        : base("throw", context) { }

      protected override bool SuppressSemicolonSuffix { get { return true; } }
      protected override IThrowStatement CreateStatement(CSharpElementFactory factory)
      {
        return (IThrowStatement) factory.CreateStatement("throw expr;");
      }

      protected override void PlaceExpression(
        IThrowStatement statement, ICSharpExpression expression, CSharpElementFactory factory)
      {
        statement.Exception.ReplaceBy(expression);
      }
    }
  }
}