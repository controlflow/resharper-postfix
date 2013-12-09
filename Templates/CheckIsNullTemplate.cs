using JetBrains.ReSharper.Feature.Services.Lookup;
using JetBrains.ReSharper.Psi.ControlFlow.CSharp;

namespace JetBrains.ReSharper.ControlFlow.PostfixCompletion.TemplateProviders
{
  [PostfixTemplate(
    templateName: "null",
    description: "Checks expression to be null",
    example: "if (expr == null)")]
  public class CheckIsNullTemplate : CheckForNullTemplateBase, IPostfixTemplate {
    public ILookupItem CreateItems(PostfixTemplateContext context) {
      var exprContext = context.OuterExpression;
      if (!exprContext.CanBeStatement) return null;

      if (!context.ForceMode) {
        if (exprContext.Type.IsUnknown) return null;
        if (!IsNullableType(exprContext.Type)) return null;
      }

      var state = CSharpControlFlowNullReferenceState.UNKNOWN;
      if (!context.ForceMode) {
        state = CheckNullabilityState(exprContext);
      }

      switch (state) {
        case CSharpControlFlowNullReferenceState.MAY_BE_NULL:
        case CSharpControlFlowNullReferenceState.UNKNOWN: {
          return new CheckForNullItem("null", exprContext, "if(expr==null)");
        }
      }

      return null;
    }
  }
}