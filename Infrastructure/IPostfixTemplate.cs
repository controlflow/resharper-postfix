using System;
using JetBrains.Annotations;
using JetBrains.Application;
using JetBrains.ReSharper.Feature.Services.Lookup;

namespace JetBrains.ReSharper.ControlFlow.PostfixCompletion
{
  public interface IPostfixTemplate
  {
    // todo: rename
    [CanBeNull] ILookupItem CreateItems([NotNull] PostfixTemplateContext context);
  }

  [AttributeUsage(AttributeTargets.Class), MeansImplicitUse]
  [BaseTypeRequired(typeof(IPostfixTemplate))]
  [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
  public sealed class PostfixTemplateAttribute : ShellComponentAttribute
  {
    [NotNull] public string TemplateName { get; private set; }
    [NotNull] public string Description { get; private set; }
    [NotNull] public string Example { get; private set; }

    public bool WorksOnTypes { get; set; }
    public bool DisabledByDefault { get; set; }

    public PostfixTemplateAttribute(
      [NotNull] string templateName, [NotNull] string description, string example = null)
    {
      TemplateName = templateName;
      Description = description;
      Example = example ?? string.Empty;
    }
  }
}