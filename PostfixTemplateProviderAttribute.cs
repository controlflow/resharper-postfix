using System;
using JetBrains.Annotations;
using JetBrains.Application;

namespace JetBrains.ReSharper.ControlFlow.PostfixCompletion
{
  [AttributeUsage(AttributeTargets.Class), MeansImplicitUse]
  [BaseTypeRequired(typeof(IPostfixTemplateProvider))]
  public sealed class PostfixTemplateProviderAttribute : ShellComponentAttribute
  {
    [NotNull] public string[] TemplateNames { get; private set; }
    [NotNull] public string Description { get; private set; }

    public bool WorksOnTypes { get; set; }
    public bool DisabledByDefault { get; set; } // todo: use me

    public PostfixTemplateProviderAttribute([NotNull] string templateName, [NotNull] string description)
    {
      TemplateNames = new[] {templateName};
      Description = description;
    }

    public PostfixTemplateProviderAttribute([NotNull] string[] templateNames, [NotNull] string description)
    {
      TemplateNames = templateNames;
      Description = description;
    }
  }
}