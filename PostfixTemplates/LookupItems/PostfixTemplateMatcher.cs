using System;
using System.Linq;
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
    public PostfixTemplateMatcher([NotNull] PostfixTemplateInfo info) : base(info) { }

    public bool IgnoreSoftOnSpace
    {
      get { return false; }
      set { throw new InvalidOperationException(); }
    }

    public MatchingResult Match(PrefixMatcher prefixMatcher, ITextControl textControl)
    {
      var text = Info.Shortcut;
      if (text == null) return new MatchingResult();

      if (text == "forEach")
      {
        var hackPrefix = HackPrefix(prefixMatcher.Prefix);
        if (hackPrefix != null)
        {
          var matcher = prefixMatcher.Factory.CreatePrefixMatcher(hackPrefix, prefixMatcher.IdentifierMatchingStyle);

          var result = matcher.Matcher(text);
          if (result == null) return null;

          return new MatchingResult(
            result.HighlightedRanges.Where(x => x < 3),
            //result.MostLikelyContinuation,
            null,
            result.AdjustedScore > 0 ? result.AdjustedScore * 2 : result.AdjustedScore / 2,
            result.OriginalScore);
        }
      }

      return prefixMatcher.Matcher(text);
    }

    [CanBeNull] private static string HackPrefix([NotNull] string prefix)
    {
      if (prefix.Length > 0 && prefix.Length <= 3 &&
          prefix.Equals("for".Substring(0, prefix.Length), StringComparison.OrdinalIgnoreCase))
      {
        return prefix + "Each";
      }

      return null;
    }
  }
}