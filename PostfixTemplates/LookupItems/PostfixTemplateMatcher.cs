using System;
using JetBrains.Annotations;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.AspectLookupItems.BaseInfrastructure;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.LookupItems;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.Match;
using JetBrains.ReSharper.PostfixTemplates.LookupItems;
using JetBrains.TextControl;

namespace JetBrains.ReSharper.PostfixTemplates.CodeCompletion
{
  public class PostfixTemplateMatcher : LookupItemAspect<PostfixTemplateInfo>, ILookupItemMatcher
  {
    public bool IgnoreSoftOnSpace
    {
      get { return false; }
      set { throw new InvalidOperationException(); }
    }

    public PostfixTemplateMatcher([NotNull] PostfixTemplateInfo info) : base(info) { }

    public MatchingResult Match(PrefixMatcher prefixMatcher, ITextControl textControl)
    {
      var text = Info.Text;
      if (text == null) return new MatchingResult();

      return prefixMatcher.Matcher(text);
    }
  }
}