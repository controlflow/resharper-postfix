using System.Collections.Generic;
using JetBrains.ReSharper.ControlFlow.PostfixCompletion.LookupItems;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.ControlFlow;
using JetBrains.ReSharper.Psi.ControlFlow.CSharp;

namespace JetBrains.ReSharper.ControlFlow.PostfixCompletion.TemplateProviders
{
  [PostfixTemplateProvider(new[] { "null", "notnull" }, "Checks expressions for nulls")]
  public class CheckForNullTemplateProvider : IPostfixTemplateProvider
  {
    // todo: loose!

    public IEnumerable<PostfixLookupItem> CreateItems(PostfixTemplateAcceptanceContext context)
    {
      if (context.ExpressionType.IsUnknown)
      {
        if (!context.LooseChecks) yield break;
      }
      else
      {
        var canBeNull = context.ExpressionType.IsNullable() ||
          (context.ExpressionType.Classify == TypeClassification.REFERENCE_TYPE);

        if (!canBeNull) yield break;
      }

      var state = CSharpControlFlowNullReferenceState.UNKNOWN;

      if (!context.LooseChecks)
      {
        IDeclaredElement declaredElement = null;
        var qualifier = context.Expression as IReferenceExpression;
        if (qualifier != null)
          declaredElement = qualifier.Reference.Resolve().DeclaredElement;

        var declaration = context.ContainingFunction;
        if (declaration != null && declaredElement != null)
        {
          var graph = CSharpControlFlowBuilder.Build(declaration);
          if (graph != null)
          {
            var result = graph.Inspect(ValueAnalysisMode.OPTIMISTIC);
            if (!result.HasComplexityOverflow)
            {
              var referenceExpression = context.ReferenceExpression;

              foreach (var element in graph.AllElements)
              if (element.SourceElement == referenceExpression)
              {
                state = result.GetVariableStateAt(element, declaredElement); break;
              }
            }
          }
        }
      }

      switch (state)
      {
        case CSharpControlFlowNullReferenceState.MAY_BE_NULL:
        case CSharpControlFlowNullReferenceState.UNKNOWN:
        {
          if (context.CanBeStatement)
          {
            yield return new PostfixLookupItem("notnull", "if ($EXPR$ != null) ");
            yield return new PostfixLookupItem("null", "if ($EXPR$ == null) ");
          }
          else
          {
            yield return new PostfixLookupItem("notnull", "$EXPR$ != null");
            yield return new PostfixLookupItem("null", "$EXPR$ == null");
          }

          break;
        }
      }
    }
  }
}