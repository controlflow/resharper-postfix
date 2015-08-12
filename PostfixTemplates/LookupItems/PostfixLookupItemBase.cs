using JetBrains.ReSharper.Feature.Services.CodeCompletion;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.LookupItems;
using JetBrains.TextControl;
using JetBrains.Util;

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
  }
}