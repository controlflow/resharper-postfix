using JetBrains.Annotations;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.LookupItems;
using JetBrains.ReSharper.PostfixTemplates.Contexts;
using JetBrains.ReSharper.PostfixTemplates.Contexts.CSharp;
using JetBrains.ReSharper.PostfixTemplates.LookupItems;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Util;

namespace JetBrains.ReSharper.PostfixTemplates.Templates.CSharp
{
  [PostfixTemplate(
    templateName: "switch",
    description: "Produces switch over integral/string type",
    example: "switch (expr)")]
  public class SwitchStatementTemplate : IPostfixTemplate<CSharpPostfixTemplateContext>
  {
    public ILookupItem CreateItem(CSharpPostfixTemplateContext context)
    {
      var isAutoCompletion = context.ExecutionContext.IsAutoCompletion;
      foreach (var expressionContext in context.Expressions)
      {
        if (!expressionContext.CanBeStatement) continue;

        if (isAutoCompletion)
        {
          // disable for constant expressions
          if (!expressionContext.Expression.ConstantValue.IsBadValue()) continue;

          var expressionType = expressionContext.Type;
          if (!expressionType.IsResolved) continue;
          if (expressionType.IsNullable())
          {
            expressionType = expressionType.GetNullableUnderlyingType();
            if (expressionType == null || !expressionType.IsResolved) continue;
          }

          if (!expressionType.IsPredefinedIntegral() &&
              !expressionType.IsEnumType()) continue;
        }

        return new SwitchItem(expressionContext);
      }

      return null;
    }

    private sealed class SwitchItem : StatementPostfixLookupItem<ISwitchStatement>
    {
      public SwitchItem([NotNull] CSharpPostfixExpressionContext context) : base("switch", context) { }

      // switch statement can't be without braces
      protected override ISwitchStatement CreateStatement(CSharpElementFactory factory,
                                                          ICSharpExpression expression)
      {
        var template = "switch($0)" + RequiredBracesTemplate;
        return (ISwitchStatement) factory.CreateStatement(template, expression);
      }
    }
  }
}