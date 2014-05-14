using System;
using JetBrains.Annotations;
using JetBrains.Application;
using JetBrains.ReSharper.Feature.Services.Lookup;

namespace JetBrains.ReSharper.PostfixTemplates
{
  public interface IPostfixTemplate
  {
    [CanBeNull] IPostfixLookupItem CreateItem([NotNull] PostfixTemplateContext context);
  }

  [AttributeUsage(AttributeTargets.Class), MeansImplicitUse]
  [BaseTypeRequired(typeof(IPostfixTemplate))]
  [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
  public sealed class PostfixTemplateAttribute : ShellComponentAttribute
  {
    public PostfixTemplateAttribute([NotNull] string templateName,
                                    [NotNull] string description,
                                    [CanBeNull] string example = null)
    {
      TemplateName = templateName;
      Description = description;
      Example = example ?? string.Empty;
    }

    [NotNull] public string TemplateName { get; private set; }
    [NotNull] public string Description { get; private set; }
    [NotNull] public string Example { get; private set; }

    public bool DisabledByDefault { get; set; }
  }
}