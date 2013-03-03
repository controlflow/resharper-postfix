using System.Collections.Generic;
using JetBrains.ReSharper.ControlFlow.PostfixCompletion.LookupItems;
using JetBrains.ReSharper.Feature.Services.Lookup;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.ControlFlow;
using JetBrains.ReSharper.Psi.ControlFlow.CSharp;

namespace JetBrains.ReSharper.ControlFlow.PostfixCompletion.TemplateProviders
{
  [PostfixTemplateProvider(new[] { "null", "notnull" }, "Checks expressions for nulls")]
  public class CheckForNullTemplateProvider : IPostfixTemplateProvider
  {
    public void CreateItems(PostfixTemplateAcceptanceContext context, ICollection<ILookupItem> consumer)
    {
      if (context.ExpressionType.IsUnknown)
      {
        if (!context.LooseChecks) return;
      }
      else
      {
        var canBeNull = context.ExpressionType.IsNullable() ||
          (context.ExpressionType.Classify == TypeClassification.REFERENCE_TYPE);

        if (!canBeNull) return;
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
            consumer.Add(new PostfixLookupItem(context, "notnull", "if ($EXPR$ != null) "));
            consumer.Add(new PostfixLookupItem(context, "null", "if ($EXPR$ == null) "));
          }
          else
          {
            consumer.Add(new PostfixLookupItem(context, "notnull", "$EXPR$ != null"));
            consumer.Add(new PostfixLookupItem(context, "null", "$EXPR$ == null"));
          }

          break;
        }
      }
    }
  }
}