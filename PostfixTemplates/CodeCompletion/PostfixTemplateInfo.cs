using System;
using System.Text;
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

    public string Text { get { return myText; } }

    public int Multiplier
    {
      get { return 0; }
    }

    public EvaluationMode EvaluationMode
    {
      get { return EvaluationMode.Light; }
      set { throw new InvalidOperationException(); }
    }

    public bool IsDynamic { get { return false; } }

#if RESHARPER92

    public int Identity { get { return 0; } }

#else

    public StringBuilder Identity
    {
      get { return new StringBuilder(myText); }
    }

#endif

    public LookupItemPlacement Placement
    {
      get { return new LookupItemPlacement(myText, PlacementLocation.Bottom, SelectionPriority.Low); }
      set { throw new InvalidOperationException(); }
    }

    public event Action<string> TextChanged
    {
      add { }
      remove { }
    }
  }
}