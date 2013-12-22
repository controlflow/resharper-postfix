using JetBrains.ReSharper.Feature.Services.Lookup;
using JetBrains.ReSharper.Psi.ControlFlow.CSharp;

namespace JetBrains.ReSharper.PostfixTemplates.Templates
{
  [PostfixTemplate(
    templateName: "null",
    description: "Checks expression to be null",
    example: "if (expr == null)")]
  public class CheckIsNullTemplate : CheckForNullTemplateBase, IPostfixTemplate
  {
    public ILookupItem CreateItem(PostfixTemplateContext context)
    {
      var expressionContext = context.OuterExpression;
      if (!expressionContext.CanBeStatement) return null;

      if (!context.IsForceMode)
      {
        var expressionType = expressionContext.Type;
        if (expressionType.IsUnknown) return null;

        if (!IsNullableType(expressionType)) return null;
      }

      var state = CSharpControlFlowNullReferenceState.UNKNOWN;
      if (!context.IsForceMode)
      {
        state = CheckNullabilityState(expressionContext);
      }

      switch (state)
      {
        case CSharpControlFlowNullReferenceState.MAY_BE_NULL:
        case CSharpControlFlowNullReferenceState.UNKNOWN:
        {
          return new CheckForNullItem("null", expressionContext, "if($0==null)");
        }
      }

      return null;
    }
  }
}