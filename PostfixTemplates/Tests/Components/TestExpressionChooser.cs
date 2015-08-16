using System;
using System.Collections.Generic;
using JetBrains.ActionManagement;
using JetBrains.Annotations;
using JetBrains.Application;
using JetBrains.DataFlow;
using JetBrains.ReSharper.PostfixTemplates.Contexts.CSharp;
using JetBrains.ReSharper.PostfixTemplates.LookupItems;
using JetBrains.TextControl;
using JetBrains.TextControl.DocumentMarkup;
using JetBrains.Threading;
using JetBrains.UI.PopupMenu;
#if RESHARPER92
using JetBrains.Application.Threading;
#endif

namespace JetBrains.ReSharper.PostfixTemplates.Components
{
  [ShellComponent]
  public sealed class TestExpressionChooser : ExpressionChooser
  {
    public TestExpressionChooser([NotNull] JetPopupMenus popupMenus, [NotNull] ShellLocks shellLocks,
                                 [NotNull] IActionManager actionManager, [NotNull] IThreading threading,
                                 [NotNull] IDocumentMarkupManager markupManager)
      : base(popupMenus, shellLocks, actionManager, threading, markupManager) { }

    public override void Execute(Lifetime lifetime, ITextControl textControl,
                                 IList<CSharpPostfixExpressionContext> expressions, string postfixText,
                                 string chooserTitle, Action<int> continuation)
    {
      continuation(0);
    }
  }
}