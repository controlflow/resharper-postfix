using System;
using JetBrains.Annotations;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.AspectLookupItems.BaseInfrastructure;
using JetBrains.ReSharper.Feature.Services.Resources;
using JetBrains.ReSharper.PostfixTemplates.LookupItems;
using JetBrains.TextControl;
using JetBrains.UI.Icons;
using JetBrains.UI.RichText;
using JetBrains.Util;

namespace JetBrains.ReSharper.PostfixTemplates.CodeCompletion
{
  // todo: maybe do text.ToLowerCase() here, to support 'notNull' and 'forEach'
  // todo: aspect?

  public class PostfixTemplatePresentation : ILookupItemPresentation
  {
    [NotNull] private readonly RichText myDisplayName;
    [CanBeNull] private RichText myDisplayTypeName;

    public PostfixTemplatePresentation([NotNull] RichText displayName)
    {
      myDisplayName = displayName;
    }

    public PostfixTemplatePresentation([NotNull] PostfixTemplateInfo info)
    {
      myDisplayName = info.Text.ToLowerInvariant();
    }

    public TextRange GetVisualReplaceRange(ITextControl textControl, TextRange nameRange)
    {
      return TextRange.InvalidRange;
    }

    public IconId Image
    {
      get { return ServicesThemedIcons.LiveTemplate.Id; }
    }

    public RichText DisplayName { get { return myDisplayName; } }

    public RichText DisplayTypeName
    {
      get { return myDisplayTypeName ?? RichText.Empty; }
      set { myDisplayTypeName = value; }
    }

    public bool CanShrink { get { return false; } }

    public bool Shrink()
    {
      throw new InvalidOperationException();
    }

    public void Unshrink()
    {
      throw new InvalidOperationException();
    }
  }
}