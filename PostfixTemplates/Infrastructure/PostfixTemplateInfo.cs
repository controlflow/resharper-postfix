using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using JetBrains.Annotations;
using JetBrains.ReSharper.Feature.Services.CodeCompletion;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.AspectLookupItems.BaseInfrastructure;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.LookupItems;
using JetBrains.ReSharper.PostfixTemplates.Contexts;
using JetBrains.Util;

namespace JetBrains.ReSharper.PostfixTemplates.LookupItems
{
  public class PostfixTemplateInfo : UserDataHolder, ILookupItemInfo
  {
    [NotNull] private readonly string myText;
    [NotNull] private readonly IList<PostfixExpressionContextImage> myImages;
    [NotNull] private readonly PostfixTemplateExecutionContext myExecutionContext;
    private readonly PostfixTemplateTarget myTarget;

    public PostfixTemplateInfo([NotNull] string text, [NotNull] IEnumerable<PostfixExpressionContext> expressions, PostfixTemplateTarget target = PostfixTemplateTarget.Expression)
    {
      myText = text;
      myTarget = target;
      myImages = expressions.ToList(x => new PostfixExpressionContextImage(x));
      myExecutionContext = expressions.First().PostfixContext.ExecutionContext; // ewww
    }

    public PostfixTemplateInfo([NotNull] string text, [NotNull] PostfixExpressionContext expression, PostfixTemplateTarget target = PostfixTemplateTarget.Expression)
    {
      myText = text;
      myTarget = target;
      myImages = new[] {new PostfixExpressionContextImage(expression)};
      myExecutionContext = expression.PostfixContext.ExecutionContext;
    }

    public string Text
    {
      get { return myText; }
    }

    [NotNull] public PostfixTemplateExecutionContext ExecutionContext
    {
      get { return myExecutionContext; }
    }

    // todo: expose ExecutionContext?
    public string ReparseString
    {
      get { return myExecutionContext.ReparseString; }
    }

    public PostfixTemplateTarget Target
    {
      get { return myTarget; }
    }

    public int Multiplier { get { return 0; } }

    public EvaluationMode EvaluationMode
    {
      get { return EvaluationMode.Light; }
      set
      {
        if (value != EvaluationMode.Light)
          throw new InvalidOperationException();
      }
    }

    public bool IsDynamic { get { return false; } }

    #region Crazy shit down here, do not expand this regions for your own safety

#if RESHARPER92

    public int Identity { get { return 0 /* no fucking idea why! */; } }

#else

    public StringBuilder Identity { get { return new StringBuilder(myText); } }

#endif

    #endregion

    [NotNull, ItemNotNull]
    public IList<PostfixExpressionContextImage> Images
    {
      get { return myImages; }
    }

    public LookupItemPlacement Placement
    {
      get { return new LookupItemPlacement(myText); }
      set { throw new InvalidOperationException(); }
    }

    public event Action<string> TextChanged
    {
      add { }
      remove { }
    }
  }

  public enum PostfixTemplateTarget
  {
    Expression,
    Statement,
    TypeUsage
  }
}