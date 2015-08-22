using System;
using JetBrains.Annotations;
using JetBrains.DocumentModel;

namespace JetBrains.ReSharper.PostfixTemplates.Contexts
{
  public sealed class PostfixExpressionContextImage
  {
    [NotNull] private readonly Type myExpressionType;
    private readonly DocumentRange myExpressionRange;
    private readonly int myExpressionIndex;

    public PostfixExpressionContextImage([NotNull] PostfixExpressionContext context)
    {
      myExpressionType = context.Expression.GetType();
      myExpressionRange = context.ExpressionRange;
      myExpressionIndex = context.PostfixContext.AllExpressions.IndexOf(context);
    }

    public int ExpressionIndex { get { return myExpressionIndex; } }

    public bool MatchesByRangeAndType([NotNull] PostfixExpressionContext context)
    {
      var startOffset = myExpressionRange.TextRange.StartOffset;
      return context.Expression.GetType() == myExpressionType
          && context.ExpressionRange.TextRange.StartOffset == startOffset;
    }
  }
}