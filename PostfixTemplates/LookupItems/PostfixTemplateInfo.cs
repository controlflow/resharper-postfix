using System;
using JetBrains.Annotations;
using JetBrains.ReSharper.Feature.Services.CodeCompletion;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.AspectLookupItems.BaseInfrastructure;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.LookupItems;
using JetBrains.Util;

namespace JetBrains.ReSharper.PostfixTemplates.CodeCompletion
{
  public class PostfixTemplateInfo : UserDataHolder, ILookupItemInfo
  {
    [NotNull] private readonly string myText;

    public PostfixTemplateInfo([NotNull] string text)
    {
      myText = text;
    }

    public int Multiplier { get { return 0; } }

    public EvaluationMode EvaluationMode
    {
      get { return EvaluationMode.Light; }
      set { throw new InvalidOperationException(); }
    }

    public bool IsDynamic { get { return false; } }

    public int Identity { get { return 0; } }

    public string Text
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