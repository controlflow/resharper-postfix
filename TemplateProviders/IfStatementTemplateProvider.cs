using System.Collections.Generic;
using JetBrains.ReSharper.ControlFlow.PostfixCompletion.LookupItems;
using JetBrains.ReSharper.Feature.Services.Lookup;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;

namespace JetBrains.ReSharper.ControlFlow.PostfixCompletion.TemplateProviders
{
  //[PostfixTemplateProvider("if", "Checks boolean expression to be 'true'")]
  //public class IfStatementTemplateProvider : IPostfixTemplateProvider
  //{
  //  // todo: detect relational expressions
  //
  //  public void CreateItems(PostfixTemplateAcceptanceContext context, ICollection<ILookupItem> consumer)
  //  {
  //    if (context.CanBeStatement)
  //    {
  //      // todo: smart caret? stay in condition when loose?
  //
  //      if (context.ExpressionType.IsBool() || context.LooseChecks)
  //        consumer.Add(new PostfixLookupItemObsolete(context, "if", "if ($EXPR$) $CARET$"));
  //    }
  //  }
  //}

  [PostfixTemplateProvider("if", "Checks boolean expression to be 'true'")]
  public class IfStatementTemplateProvider2 : IPostfixTemplateProvider
  {
    public void CreateItems(PostfixTemplateAcceptanceContext context, ICollection<ILookupItem> consumer)
    {
      foreach (var expression in context.PossibleExpressions)
      {
        if (expression.CanBeStatement)
        {
          if (expression.ExpressionType.IsBool() ||
              context.LooseChecks ||
              expression.Expression is IRelationalExpression)
          {
            consumer.Add(new PostfixStatementLookupItem("if", context, expression));
          }
        }
      }

      
    }
  }
}