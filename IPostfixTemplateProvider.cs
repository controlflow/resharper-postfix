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
    [NotNull] public string TemplateName { get; private set; }
    [NotNull] public string Description { get; private set; }

    public PostfixTemplateProviderAttribute([NotNull] string templateName, [NotNull] string description)
    {
      TemplateName = templateName;
      Description = description;
    }
  }

  public interface IPostfixTemplateProvider
  {
    // todo: different behavior for auto/basic completion?
    [NotNull] IEnumerable<PostfixLookupItem> CreateItems(
      [NotNull] ICSharpExpression expression, [NotNull] IType expressionType, bool canBeStatement);
  }
}