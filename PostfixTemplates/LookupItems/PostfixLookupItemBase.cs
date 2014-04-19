using System;
using JetBrains.TextControl;
using JetBrains.Util;
#if RESHARPER8
using JetBrains.ReSharper.Feature.Services.Lookup;
#elif RESHARPER9
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

#if RESHARPER9
    public ILookupItemPlacement Placement
    {
      get { return null; }
      set { GC.KeepAlive(value); }
    }
#endif
  }
}