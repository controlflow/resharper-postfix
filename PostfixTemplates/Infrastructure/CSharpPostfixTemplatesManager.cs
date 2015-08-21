using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.Application;
using JetBrains.ReSharper.PostfixTemplates.Contexts.CSharp;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.PostfixTemplates
{
  [ShellComponent]
  public class CSharpPostfixTemplatesManager : PostfixTemplatesManager<CSharpPostfixTemplateContext>
  {
    public CSharpPostfixTemplatesManager(
      [NotNull] IEnumerable<IPostfixTemplate<CSharpPostfixTemplateContext>> providers, [NotNull] LanguageManager languageManager)
      : base(providers, languageManager) { }
  }
}