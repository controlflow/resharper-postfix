using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.Application;
using JetBrains.ReSharper.ControlFlow.PostfixCompletion.LookupItems;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;

namespace JetBrains.ReSharper.ControlFlow.PostfixCompletion
{
  [AttributeUsage(AttributeTargets.Class), MeansImplicitUse]
  [BaseTypeRequired(typeof(IPostfixTemplateProvider))]
  public sealed class PostfixTemplateProviderAttribute : ShellComponentAttribute
  {
    [NotNull] public string[] TemplateNames { get; private set; }
    [NotNull] public string Description { get; private set; }

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

  public interface IPostfixTemplateProvider
  {
    // todo: different behavior for auto/basic completion?
    // todo: extract parameters to 'PostfixTemplateAcceptanceContext'

    [NotNull] IEnumerable<PostfixLookupItem> CreateItems(
      [NotNull] ICSharpExpression expression, [NotNull] IType expressionType, bool canBeStatement);
  }
}