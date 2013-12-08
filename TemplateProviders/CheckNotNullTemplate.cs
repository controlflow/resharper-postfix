using JetBrains.ReSharper.Feature.Services.Lookup;
using JetBrains.ReSharper.Psi.ControlFlow.CSharp;

namespace JetBrains.ReSharper.ControlFlow.PostfixCompletion.TemplateProviders
{
  [PostfixTemplate(
    templateName: "notnull",
    description: "Checks expression to be not-null",
    example: "if (expr != null)")]
  public class CheckNotNullTemplate : CheckForNullTemplateBase, IPostfixTemplate {
    public ILookupItem CreateItems(PostfixTemplateContext context) {
      var expressionContext = context.OuterExpression;
      if (!expressionContext.CanBeStatement) return null;

      if (!context.ForceMode) {
        if (expressionContext.Type.IsUnknown) return null;
        if (!IsNullableType(expressionContext.Type)) return null;
      }

      var state = CSharpControlFlowNullReferenceState.UNKNOWN;
      if (!context.ForceMode) {
        state = CheckNullabilityState(expressionContext);
      }

      switch (state) {
        case CSharpControlFlowNullReferenceState.MAY_BE_NULL:
        case CSharpControlFlowNullReferenceState.UNKNOWN: {
          return new CheckForNullItem("notNull", expressionContext, "if(expr!=null)");
        }
      }

      return null;
    }
  }
}