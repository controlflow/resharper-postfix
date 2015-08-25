using JetBrains.Annotations;
using JetBrains.ReSharper.PostfixTemplates.Contexts.CSharp;
using JetBrains.ReSharper.PostfixTemplates.LookupItems;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Util;

namespace JetBrains.ReSharper.PostfixTemplates.Templates.CSharp
{
  // todo: emit case label and put hotspot over value?

  [PostfixTemplate(
    templateName: "switch",
    description: "Produces switch over integral/string type",
    example: "switch (expr)")]
  public class SwitchStatementTemplate : IPostfixTemplate<CSharpPostfixTemplateContext>
  {
    public PostfixTemplateInfo TryCreateInfo(CSharpPostfixTemplateContext context)
    {
      var isPreciseMode = context.ExecutionContext.IsPreciseMode;

      foreach (var expressionContext in context.Expressions)
      {
        if (!expressionContext.CanBeStatement) continue;

        if (isPreciseMode)
        {
          // todo: decompose

          // disable for constant expressions
          if (!expressionContext.Expression.ConstantValue.IsBadValue()) continue;

          var expressionType = expressionContext.Type;
          if (!expressionType.IsResolved) continue;

          if (expressionType.IsNullable())
          {
            expressionType = expressionType.GetNullableUnderlyingType();
            if (expressionType == null || !expressionType.IsResolved) continue;
          }

          if (!expressionType.IsPredefinedIntegral() && !expressionType.IsEnumType()) continue;
        }

        return new PostfixTemplateInfo("switch", expressionContext);
      }

      return null;
    }

    public PostfixTemplateBehavior CreateBehavior(PostfixTemplateInfo info)
    {
      return new CSharpPostfixSwitchStatementBehavior(info);
    }

    private sealed class CSharpPostfixSwitchStatementBehavior : CSharpStatementPostfixTemplateBehavior<ISwitchStatement>
    {
      public CSharpPostfixSwitchStatementBehavior([NotNull] PostfixTemplateInfo info) : base(info) { }

      // switch statement can't be without braces
      protected override ISwitchStatement CreateStatement(CSharpElementFactory factory, ICSharpExpression expression)
      {
        var template = "switch($0)" + RequiredBracesTemplate;
        return (ISwitchStatement) factory.CreateStatement(template, expression);
      }
    }
  }
}