using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.ReSharper.ControlFlow.PostfixCompletion.LookupItems;
using JetBrains.ReSharper.Feature.Services.Lookup;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ControlFlow;
using JetBrains.ReSharper.Psi.ControlFlow.CSharp;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.ControlFlow.PostfixCompletion.TemplateProviders
{
  // todo: split?

  [PostfixTemplateProvider(new[] { "null", "notnull" }, "Checks expressions for nulls")]
  public class CheckForNullTemplateProvider : IPostfixTemplateProvider
  {
    public void CreateItems(PostfixTemplateAcceptanceContext context, ICollection<ILookupItem> consumer)
    {
      var outer = context.PossibleExpressions.LastOrDefault();
      if (outer == null || !outer.CanBeStatement) return;

      if (outer.ExpressionType.IsUnknown)
      {
        if (!context.ForceMode) return;
      }
      else
      {
        if (!IsNullableType(outer.ExpressionType)) return;
      }

      var state = CSharpControlFlowNullReferenceState.UNKNOWN;

      if (!context.ForceMode)
      {
        var declaredElement = outer.ReferencedElement;
        var declaration = outer.Expression.GetContainingNode<ICSharpFunctionDeclaration>();
        if (declaration != null && declaredElement != null)
        {
          if (declaration.IsPhysical())
          {
            // todo: can always be physical?
            var graph = CSharpControlFlowBuilder.Build(declaration);
            if (graph != null)
            {
              var result = graph.Inspect(ValueAnalysisMode.OPTIMISTIC);
              if (!result.HasComplexityOverflow)
              {
                var referenceExpression = outer.Expression;

                foreach (var element in graph.AllElements)
                  if (element.SourceElement == referenceExpression)
                  {
                    state = result.GetVariableStateAt(element, declaredElement);
                    break;
                  }
              }
            }
          }
          else
          {
            System.GC.KeepAlive(this);
            MessageBox.ShowError("LOL");
          }
        }
      }

      switch (state)
      {
        case CSharpControlFlowNullReferenceState.MAY_BE_NULL:
        case CSharpControlFlowNullReferenceState.UNKNOWN:
        {
          consumer.Add(new LookupItem("notnull", outer, "expr != null"));
          consumer.Add(new LookupItem("null", outer, "expr == null"));
          break;
        }
      }
    }

    private static bool IsNullableType([NotNull] IType type)
    {
      if (type.IsNullable()) return true;

      return type.Classify == TypeClassification.REFERENCE_TYPE;
    }

    private sealed class LookupItem : IfStatementPostfixLookupItemBase
    {
      private readonly string myCondition;

      public LookupItem(
        [NotNull] string shortcut, [NotNull] PrefixExpressionContext context, [NotNull] string condition)
        : base(shortcut, context)
      {
        myCondition = condition;
      }

      protected override void PlaceExpression(
        IIfStatement statement, ICSharpExpression expression, CSharpElementFactory factory)
      {
        var checkedExpression = (IEqualityExpression) factory.CreateExpression(myCondition);
        checkedExpression = statement.Condition.ReplaceBy(checkedExpression);
        checkedExpression.LeftOperand.ReplaceBy(expression);
      }
    }
  }
}