using System;
using JetBrains.Annotations;
using JetBrains.DocumentModel;
using JetBrains.ReSharper.PostfixTemplates.Contexts;
using JetBrains.ReSharper.PostfixTemplates.Contexts.CSharp;

namespace JetBrains.ReSharper.PostfixTemplates.LookupItems
{
  public sealed class PostfixExpressionContextImage
  {
    [NotNull] private readonly Type myExpressionType;
    private readonly DocumentRange myExpressionRange;
    private readonly int myContextIndex;

    public PostfixExpressionContextImage([NotNull] PostfixExpressionContext context)
    {
      myExpressionType = context.Expression.GetType();
      myExpressionRange = context.ExpressionRange;
      myContextIndex = context.PostfixContext.Expressions.IndexOf(context);
    }

    public int ContextIndex { get { return myContextIndex; } }

    public bool MatchesByRangeAndType([NotNull] CSharpPostfixExpressionContext context)
    {
      var startOffset = myExpressionRange.TextRange.StartOffset;
      return context.Expression.GetType() == myExpressionType
          && context.ExpressionRange.TextRange.StartOffset == startOffset;
    }
  }
}