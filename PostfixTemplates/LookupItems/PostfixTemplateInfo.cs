using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.ReSharper.Feature.Services.CodeCompletion;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.AspectLookupItems.BaseInfrastructure;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.LookupItems;
using JetBrains.ReSharper.PostfixTemplates.Contexts;
using JetBrains.Util;

namespace JetBrains.ReSharper.PostfixTemplates.CodeCompletion
{
  public class PostfixTemplateInfo : UserDataHolder, ILookupItemInfo
  {
    [NotNull] private readonly string myText;
    private readonly PostfixTemplateTarget myTarget;

    // todo: store expressions

    public PostfixTemplateInfo([NotNull] string text, [NotNull] IEnumerable<PostfixExpressionContext> expressions, PostfixTemplateTarget target = PostfixTemplateTarget.Expression)
    {
      myText = text;
      myTarget = target;
    }

    public PostfixTemplateInfo([NotNull] string text, [NotNull] PostfixExpressionContext expression, PostfixTemplateTarget target = PostfixTemplateTarget.Expression)
    {
      myText = text;
      myTarget = target;
    }

    public string Text
    {
      get { return myText; }
    }

    public string ReparseString
    {
      get { return "aaa__"; }
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

    public int Identity { get { return 0; } }

    public object Shortcut
    {
      get { return myText; }
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
}