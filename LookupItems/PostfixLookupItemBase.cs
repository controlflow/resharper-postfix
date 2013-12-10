using JetBrains.ReSharper.Feature.Services.Lookup;
using JetBrains.TextControl;
using JetBrains.Util;

namespace JetBrains.ReSharper.ControlFlow.PostfixCompletion.LookupItems
{
  public abstract class PostfixLookupItemBase
  {
    public bool CanShrink { get { return false; } }
    public bool Shrink() { return false; }
    public void Unshrink() { }

    public TextRange GetVisualReplaceRange(ITextControl textControl, TextRange nameRange)
    {
      // note: prefix highlighter disallows highlighting to be any position
      return TextRange.InvalidRange;
    }

    public bool AcceptIfOnlyMatched(LookupItemAcceptanceContext acceptanceContext)
    {
      return false;
    }

    public int Multiplier { get; set; }
    public bool IsDynamic { get { return false; } }
    public bool IgnoreSoftOnSpace { get; set; }
  }
}