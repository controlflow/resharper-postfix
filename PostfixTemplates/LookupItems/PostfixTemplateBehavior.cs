using JetBrains.Annotations;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.AspectLookupItems.BaseInfrastructure;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.LookupItems;
using JetBrains.ReSharper.Feature.Services.Lookup;
using JetBrains.ReSharper.Resources.Shell;
using JetBrains.TextControl;
using JetBrains.Util;

namespace JetBrains.ReSharper.PostfixTemplates.CodeCompletion
{
  public class PostfixTemplateBehavior : LookupItemAspect<PostfixTemplateInfo>, ILookupItemBehavior
  {
    public PostfixTemplateBehavior([NotNull] PostfixTemplateInfo info) : base(info)
    {

    }

    public bool AcceptIfOnlyMatched(LookupItemAcceptanceContext itemAcceptanceContext)
    {
      return false;
    }

    public virtual void Accept(ITextControl textControl, TextRange nameRange, LookupItemInsertType lookupItemInsertType, Suffix suffix, ISolution solution, bool keepCaretStill)
    {
      // todo: support insertion type
      // todo: what is 'keepCaretStill'?


      using (WriteLockCookie.Create())
      {
        textControl.Document.ReplaceText(nameRange, Info.Text);
      }

      var endOffset = nameRange.StartOffset + Info.Text.Length;
      textControl.Caret.MoveTo(endOffset, CaretVisualPlacement.DontScrollIfVisible);

      suffix.Playback(textControl);
    }
  }
}