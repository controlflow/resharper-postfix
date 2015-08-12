using System;
using System.Collections.Generic;
using JetBrains.ActionManagement;
using JetBrains.Annotations;
using JetBrains.Application;
using JetBrains.CommonControls;
using JetBrains.DataFlow;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.Hotspots;
using JetBrains.TextControl;
using JetBrains.TextControl.DocumentMarkup;
using JetBrains.Threading;
using JetBrains.UI.PopupMenu;
using JetBrains.Util;
#if RESHARPER8
using JetBrains.IDE;
using JetBrains.ReSharper.Daemon.Src.Bulbs.Resources;
#elif RESHARPER9
using JetBrains.TextControl.Layout;
using JetBrains.ReSharper.Feature.Services.Resources;
using JetBrains.ReSharper.Resources.Shell;
#if RESHARPER92
using JetBrains.Application.Threading;
#endif
#endif

namespace JetBrains.ReSharper.PostfixTemplates.LookupItems
{
  [ShellComponent]
  public class ExpressionChooser
  {
    [NotNull] private readonly JetPopupMenus myPopupMenus;
    [NotNull] private readonly ShellLocks myShellLocks;
    [NotNull] private readonly IActionManager myActionManager;
    [NotNull] private readonly IThreading myThreading;
    [NotNull] private readonly IDocumentMarkupManager myMarkupManager;

    [NotNull] private static readonly Key HighlightingKey = new Key(ChooserName);
    [NotNull] private const string ChooserName = "PostfixTemplates.ExpressionChooser";

    public ExpressionChooser([NotNull] JetPopupMenus popupMenus, [NotNull] ShellLocks shellLocks,
                             [NotNull] IActionManager actionManager, [NotNull] IThreading threading,
                             [NotNull] IDocumentMarkupManager markupManager)
    {
      myPopupMenus = popupMenus;
      myShellLocks = shellLocks;
      myActionManager = actionManager;
      myMarkupManager = markupManager;
      myThreading = threading;
    }

    public virtual void Execute([NotNull] Lifetime lifetime, [NotNull] ITextControl textControl,
                                [NotNull] IList<PrefixExpressionContext> expressions,
                                [NotNull] string postfixText, [NotNull] string chooserTitle,
                                [NotNull] Action<int> continuation)
    {
      #pragma warning disable 618
      if (Shell.Instance.IsTestShell)
      #pragma warning restore 618
      {
        continuation(0);
        return;
      }

      var popupMenu = myPopupMenus.CreateWithLifetime(lifetime);

      popupMenu.Caption.Value = WindowlessControl.Create(chooserTitle);
      popupMenu.PopupWindowContext = new TextControlPopupWindowContext(
        lifetime, textControl, myShellLocks, myActionManager);

      // advise selected element to highlight expression
      popupMenu.SelectedItemKey.Change.Advise(lifetime, args =>
      {
        if (!args.HasNew) return;

        var menuItem = args.New as SimpleMenuItem;
        if (menuItem == null) return;

        var range = menuItem.Tag as TextRange?;
        if (range != null)
        {
          UpdateHighlighting(textControl, range.Value);
        }
      });

      // build menu items from expressions
      var items = new LocalList<SimpleMenuItem>(expressions.Count);
      var index = 0;

      foreach (var expressionContext in expressions)
      {
        TextRange range;
        var expressionText = PresentExpression(expressionContext, postfixText, out range);

        var itemIndex = index++;
        // ReSharper disable once UseObjectOrCollectionInitializer
        var menuItem = new SimpleMenuItem(
          expressionText, BulbThemedIcons.YellowBulb.Id, () => continuation(itemIndex));

        menuItem.Tag = range;
        items.Add(menuItem);
      }

      popupMenu.SetItems(items.ToArray());

      var definition = Lifetimes.Define(lifetime);

      // handle menu close
      definition.Lifetime.AddAction(() =>
      {
        UpdateHighlighting(textControl, TextRange.InvalidRange);
      });

      popupMenu.Show(JetPopupMenu.ShowWhen.AutoExecuteIfSingleItem, definition);
    }

    [NotNull]
    private static string PresentExpression(
      [NotNull] PrefixExpressionContext context, [NotNull] string postfixText, out TextRange range)
    {
      var text = context.Expression.GetText();
      range = context.ExpressionRange.TextRange;

      if (context.Expression.Contains(context.PostfixContext.Reference))
      {
        var originalSize = text.Length;

        // "x > 0.par" => "x > 0"
        if (text.EndsWith(postfixText, StringComparison.OrdinalIgnoreCase))
          text = text.Substring(0, text.Length - postfixText.Length).TrimEnd();

        var delta = originalSize - text.Length;
        if (delta >= 0) range = range.ExtendRight(-delta);
      }

      text = text.ReplaceNewLines().TrimStart();

      while (true) // "aa\n         && bb" => "aa && bb"
      {
        var reduced = text.Replace("  ", " ");
        if (reduced.Length < text.Length) text = reduced;
        else break;
      }

      const int textLength = 50;
      if (text.Length > textLength)
      {
        text = text.Substring(0, textLength) + "…";
      }

      return text;
    }

    private void UpdateHighlighting([NotNull] ITextControl textControl, TextRange expressionRange)
    {
      myThreading.ExecuteOrQueue(ChooserName, () =>
      {
        using (ReadLockCookie.Create())
        {
          var documentMarkup = myMarkupManager.GetMarkupModel(textControl.Document);
          foreach (var highlighter in documentMarkup.GetHighlightersEnumerable(HighlightingKey))
          {
            documentMarkup.RemoveHighlighter(highlighter);
            break;
          }

          if (expressionRange.IsValid)
          {
            documentMarkup.AddHighlighter(
              HighlightingKey, expressionRange, AreaType.LINES_IN_RANGE, 0,
              HotspotSessionUi.CURRENT_HOTSPOT_HIGHLIGHTER,
              ErrorStripeAttributes.Empty, null);
          }
        }
      });
    }
  }
}