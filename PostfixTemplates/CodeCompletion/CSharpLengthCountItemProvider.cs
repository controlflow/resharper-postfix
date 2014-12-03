using JetBrains.Annotations;
using JetBrains.Application.Settings;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.CodeCompletion;
using JetBrains.ReSharper.Feature.Services.CSharp.CodeCompletion.Infrastructure;
using JetBrains.ReSharper.Feature.Services.Lookup;
using JetBrains.ReSharper.Feature.Services.Resources;
using JetBrains.ReSharper.Feature.Services.Tips;
using JetBrains.ReSharper.PostfixTemplates.Settings;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.TextControl;
using JetBrains.UI.Icons;
using JetBrains.UI.RichText;
using JetBrains.Util;
#if RESHARPER8
using ILookupItem = JetBrains.ReSharper.Feature.Services.Lookup.ILookupItem;
#elif RESHARPER9
using JetBrains.Threading;
using JetBrains.ReSharper.Resources.Shell;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.LookupItems;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.Match;
using JetBrains.ReSharper.Features.Intellisense.CodeCompletion.CSharp.Rules;
using ILookupItem = JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.LookupItems.ILookupItem;
#endif

namespace JetBrains.ReSharper.PostfixTemplates.CodeCompletion
{
  [Language(typeof(CSharpLanguage))]
  public class CSharpLengthCountItemProvider : CSharpItemsProviderBase<CSharpCodeCompletionContext>
  {
    protected override bool IsAvailable(CSharpCodeCompletionContext context)
    {
      var completionType = context.BasicContext.CodeCompletionType;
      return completionType == CodeCompletionType.AutomaticCompletion
          || completionType == CodeCompletionType.BasicCompletion;
    }

    private const string Length = "Length", Count = "Count";

    protected override void TransformItems(CSharpCodeCompletionContext context, GroupedItemsCollector collector)
    {
      var referenceExpression = context.UnterminatedContext.ToReferenceExpression() ??
                                context.TerminatedContext.ToReferenceExpression();
      if (referenceExpression == null) return;

      var settingsStore = referenceExpression.GetSettingsStore();
      if (!settingsStore.GetValue(PostfixSettingsAccessor.ShowLengthCountItems)) return;

      var interestingItems = new LocalList<ILookupItem>();

      foreach (var item in collector.Items)
      {
        switch (item.Identity)
        {
          case Length: break;
          case Count: break;
          default: continue;
        }

        var instance = item.GetDeclaredElement();
        if (instance == null) continue;

        var property = instance.Element as IProperty;
        if (property != null && property.Type.IsResolved && property.Type.IsInt())
        {
          interestingItems.Add(item);
        }
      }

      if (interestingItems.Count == 1)
      {
        var lookupItem = interestingItems[0];
        var text = (lookupItem.Identity == Count) ? Length : Count;

        collector.AddAtDefaultPlace(new FakeLookupElement(text, lookupItem));
      }
    }

    private sealed class FakeLookupElement : ILookupItem
    {
      [NotNull] private readonly string myFakeText;
      [NotNull] private readonly ILookupItem myRealItem;

      public FakeLookupElement([NotNull] string fakeText, [NotNull] ILookupItem realItem)
      {
        myRealItem = realItem;
        myFakeText = fakeText;
      }

      public bool AcceptIfOnlyMatched(LookupItemAcceptanceContext itemAcceptanceContext)
      {
        return myRealItem.AcceptIfOnlyMatched(itemAcceptanceContext);
      }

#if RESHARPER9

      public MatchingResult Match(PrefixMatcher prefixMatcher, ITextControl textControl)
      {
        return prefixMatcher.Matcher(myFakeText);
      }

#elif RESHARPER8

      public MatchingResult Match(string prefix, ITextControl textControl)
      {
        var matcher = TextLookupItemBase.GetPrefixMatcherEx(prefix, textControl);
        if (matcher != null) return matcher(myFakeText);

        return new MatchingResult();
      }

#endif

      public IconId Image
      {
        get { return ServicesThemedIcons.LiveTemplate.Id; }
      }

      public void Accept(ITextControl textControl, TextRange nameRange, LookupItemInsertType insertType,
                         Suffix suffix, ISolution solution, bool keepCaretStill)
      {
        const string template = "Plugin.ControlFlow.PostfixTemplates.<{0}>";
        var featureId = string.Format(template, myFakeText.ToLowerInvariant());
        TipsManager.Instance.FeatureIsUsed(featureId, textControl.Document, solution);

        myRealItem.Accept(textControl, nameRange, insertType, suffix, solution, keepCaretStill);
      }

      public TextRange GetVisualReplaceRange(ITextControl textControl, TextRange nameRange)
      {
        return myRealItem.GetVisualReplaceRange(textControl, nameRange);
      }

      public RichText DisplayName { get { return myFakeText; } }
      public RichText DisplayTypeName { get { return myRealItem.DisplayTypeName; } }

      public bool IsDynamic { get { return myRealItem.IsDynamic; } }

      public string Identity { get { return myFakeText; } }

      public bool CanShrink
      {
        get
        {
#if RESHARPER8
          return myRealItem.CanShrink;
#elif RESHARPER9
          var canShrink = false;
          ReentrancyGuard.Current.Execute("CanShrink", () =>
          {
            using (ReadLockCookie.Create())
              canShrink = myRealItem.CanShrink;
          });

          return canShrink;
#endif
        }
      }

      public bool Shrink() { return myRealItem.Shrink(); }
      public void Unshrink() { myRealItem.Unshrink(); }

      public int Multiplier
      {
        get { return myRealItem.Multiplier; }
        set { myRealItem.Multiplier = value; }
      }

      public bool IgnoreSoftOnSpace
      {
        get { return myRealItem.IgnoreSoftOnSpace; }
        set { myRealItem.IgnoreSoftOnSpace = value; }
      }

#if RESHARPER8

      public string OrderingString
      {
        get { return myFakeText; }
      }

#elif RESHARPER9

      public bool IsStable
      {
        get { return true; }
        set { }
      }

      public EvaluationMode Mode
      {
        get { return EvaluationMode.Light; }
        set { }
      }

      private LookupItemPlacement myPlacement;

      public LookupItemPlacement Placement
      {
        get { return myPlacement ?? (myPlacement = new GenericLookupItemPlacement(myFakeText)); }
        set { myPlacement = value; }
      }

#endif
    }
  }
}