using JetBrains.Annotations;
using JetBrains.TextControl;
using JetBrains.Util;
#if RESHARPER8
using JetBrains.ReSharper.Feature.Services.Lookup;
#elif RESHARPER9
using JetBrains.ReSharper.Feature.Services.CodeCompletion;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.LookupItems;
#endif

namespace JetBrains.ReSharper.PostfixTemplates.LookupItems
{
  public abstract class PostfixLookupItemBase
  {
    public bool CanShrink { get { return false; } }
    public bool Shrink() { return false; }
    public void Unshrink() { }

    public TextRange GetVisualReplaceRange(ITextControl textControl, TextRange nameRange)
    {
      return TextRange.InvalidRange;
    }

    public bool AcceptIfOnlyMatched(LookupItemAcceptanceContext acceptanceContext)
    {
      return false;
    }

    public int Multiplier { get; set; }
    public bool IsDynamic { get { return false; } }
    public bool IgnoreSoftOnSpace { get; set; }

#if RESHARPER8

    public string OrderingString
    {
      get { return Identity; }
    }

#elif RESHARPER9

    public bool IsStable
    {
      get { return true; }
      // ReSharper disable once ValueParameterNotUsed
      set { }
    }

    public EvaluationMode Mode
    {
      get { return EvaluationMode.Light; }
      // ReSharper disable once ValueParameterNotUsed
      set { }
    }

  #if RESHARPER92
  #elif RESHARPER91

    private LookupItemPlacement myPlacement;

    public LookupItemPlacement Placement
    {
      get { return myPlacement ?? (myPlacement = new LookupItemPlacement(Identity)); }
      set { myPlacement = value; }
    }

  #else

    private LookupItemPlacement myPlacement;

    public LookupItemPlacement Placement
    {
      get { return myPlacement ?? (myPlacement = new GenericLookupItemPlacement(Identity)); }
      set { myPlacement = value; }
    }

  #endif

#endif
  }
}