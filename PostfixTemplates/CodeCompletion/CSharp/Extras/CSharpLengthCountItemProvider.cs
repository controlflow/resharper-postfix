using JetBrains.Annotations;
using JetBrains.Application.Settings;
using JetBrains.ReSharper.Feature.Services.CodeCompletion;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.AspectLookupItems.BaseInfrastructure;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.AspectLookupItems.Info;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.LookupItems;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.Match;
using JetBrains.ReSharper.Feature.Services.CSharp.CodeCompletion.Infrastructure;
using JetBrains.ReSharper.Features.Intellisense.CodeCompletion.CSharp.AspectLookupItems;
using JetBrains.ReSharper.Features.Intellisense.CodeCompletion.CSharp.Rules;
using JetBrains.ReSharper.PostfixTemplates.Settings;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.TextControl;

namespace JetBrains.ReSharper.PostfixTemplates.CodeCompletion.CSharp
{
  [Language(typeof(CSharpLanguage))]
  public class CSharpLengthCountItemProvider : CSharpItemsProviderBase<CSharpCodeCompletionContext>
  {
    protected override bool IsAvailable(CSharpCodeCompletionContext context)
    {
      return context.BasicContext.CodeCompletionType == CodeCompletionType.BasicCompletion;
    }

    private const string LENGTH = "Length", COUNT = "Count";

    protected override void TransformItems(CSharpCodeCompletionContext context, GroupedItemsCollector collector)
    {
      var referenceExpression = context.UnterminatedContext.ToReferenceExpression() ??
                                context.TerminatedContext.ToReferenceExpression();
      if (referenceExpression == null) return;

      var settingsStore = referenceExpression.GetSettingsStore();
      if (!settingsStore.GetValue(PostfixSettingsAccessor.ShowLengthCountItems)) return;


      IAspectLookupItem<CSharpDeclaredElementInfo> existingItem = null;

      foreach (var lookupItem in collector.Items)
      {
        var aspectLookupItem = lookupItem as IAspectLookupItem<CSharpDeclaredElementInfo>;
        if (aspectLookupItem != null && IsLengthOrCountProperty(aspectLookupItem.Info))
        {
          // do nothing if both 'Length' and 'Count' or multiple 'Length'/'Count' items exists
          if (existingItem != null) return;

          existingItem = aspectLookupItem;
        }
      }

      if (existingItem != null)
      {
        var invertedItem = LookupItemFactory
          .CreateLookupItem(existingItem.Info)
          .WithBehavior(item => existingItem.Behavior)
          .WithMatcher(item => new SimpleTextualMatcher(InvertName(item.Info)))
          .WithPresentation(item => new PostfixTemplatePresentation(InvertName(item.Info)));

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

    private sealed class SimpleTextualMatcher : ILookupItemMatcher
    {
      [NotNull] private readonly string myText;

      public SimpleTextualMatcher([NotNull] string text) { myText = text; }

      public bool IgnoreSoftOnSpace { get; set; }

      public MatchingResult Match(PrefixMatcher prefixMatcher, ITextControl textControl)
      {
        return prefixMatcher.Matcher(myText);
      }
    }
  }
}