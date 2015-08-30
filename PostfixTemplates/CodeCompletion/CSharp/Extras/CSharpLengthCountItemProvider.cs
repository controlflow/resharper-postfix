using JetBrains.Annotations;
using JetBrains.Application.Settings;
using JetBrains.ReSharper.Feature.Services.CodeCompletion;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.AspectLookupItems.BaseInfrastructure;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.AspectLookupItems.Info;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.AspectLookupItems.Matchers;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.AspectLookupItems.Presentations;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.LookupItems;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.Match;
using JetBrains.ReSharper.Feature.Services.CSharp.CodeCompletion.Infrastructure;
using JetBrains.ReSharper.Feature.Services.Resources;
using JetBrains.ReSharper.Features.Intellisense.CodeCompletion.CSharp.AspectLookupItems;
using JetBrains.ReSharper.Features.Intellisense.CodeCompletion.CSharp.Rules;
using JetBrains.ReSharper.PostfixTemplates.Settings;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Text;
using JetBrains.TextControl;
using JetBrains.UI.Icons;
using JetBrains.Util;

namespace JetBrains.ReSharper.PostfixTemplates.CodeCompletion.CSharp
{
  [Language(typeof(CSharpLanguage))]
  public class CSharpLengthCountItemProvider : CSharpItemsProviderBase<CSharpCodeCompletionContext>
  {
    protected override bool IsAvailable(CSharpCodeCompletionContext context)
    {
      return context.BasicContext.CodeCompletionType == CodeCompletionType.BasicCompletion;
    }

    private const string LENGTH = "Length";
    private const string COUNT = "Count";

    protected override void TransformItems(CSharpCodeCompletionContext context, GroupedItemsCollector collector)
    {
      var referenceExpression = context.UnterminatedContext.ToReferenceExpression() ??
                                context.TerminatedContext.ToReferenceExpression();
      if (referenceExpression == null) return;

      var settingsStore = referenceExpression.GetSettingsStore();
      if (!settingsStore.GetValue(PostfixTemplatesSettingsAccessor.ShowLengthCountItems)) return;

      CSharpDeclaredElementInfo existingInfo = null;

      foreach (var lookupItem in collector.Items)
      {
        var aspectLookupItem = lookupItem as IAspectLookupItem<CSharpDeclaredElementInfo>;
        if (aspectLookupItem != null && IsLengthOrCountProperty(aspectLookupItem.Info))
        {
          // do nothing if both 'Length' and 'Count' or multiple 'Length'/'Count' items exists
          if (existingInfo != null) return;

          existingInfo = aspectLookupItem.Info;
        }
      }

      if (existingInfo != null)
      {
        var invertedInfo = new CSharpDeclaredElementInfo(
          existingInfo.ShortName, existingInfo.PreferredDeclaredElement.NotNull(),
          context.BasicContext.LookupItemsOwner, context, context.BasicContext);

        invertedInfo.Ranges = context.CompletionRanges;
        invertedInfo.Placement = new LookupItemPlacement(InvertName(existingInfo));

        var invertedItem = LookupItemFactory.CreateLookupItem(invertedInfo)
          .WithPresentation(item => new InvertedItemPresentation(item.Info))
          .WithMatcher(item => new InvertedTextualMatcher(item.Info))
          .WithBehavior(item => new CSharpDeclaredElementBehavior<CSharpDeclaredElementInfo>(item.Info));

        collector.Add(invertedItem);
      }
    }

    private static bool IsLengthOrCountProperty([NotNull] DeclaredElementInfo declaredElementInfo)
    {
      switch (declaredElementInfo.ShortName)
      {
        case LENGTH:
        case COUNT:
          break;

        default:
          return false;
      }

      var preferredElement = declaredElementInfo.PreferredDeclaredElement;
      if (preferredElement == null) return false;

      var property = preferredElement.Element as IProperty;
      if (property == null) return false;

      var propertyType = property.Type;
      return propertyType.IsResolved && propertyType.IsInt();
    }

    [NotNull]
    private static string InvertName([NotNull] DeclaredElementInfo info)
    {
      return (info.ShortName == COUNT) ? LENGTH : COUNT;
    }

    private sealed class InvertedTextualMatcher : ILookupItemMatcher
    {
      [NotNull] private readonly string myText;

      public InvertedTextualMatcher([NotNull] CSharpDeclaredElementInfo info)
      {
        myText = InvertName(info);
      }

      public bool IgnoreSoftOnSpace { get; set; }

      public MatchingResult Match(PrefixMatcher prefixMatcher, ITextControl textControl)
      {
        return prefixMatcher.Matcher(myText);
      }
    }

    private sealed class InvertedItemPresentation : DeclaredElementPresentation<CSharpDeclaredElementInfo>
    {
      public InvertedItemPresentation([NotNull] CSharpDeclaredElementInfo info)
        : base(InvertName(info), info) { }

      public override IconId Image
      {
        get { return ServicesThemedIcons.LiveTemplate.Id; }
      }
    }
  }
}