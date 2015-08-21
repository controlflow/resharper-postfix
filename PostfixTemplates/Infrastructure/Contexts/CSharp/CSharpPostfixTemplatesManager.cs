using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;

namespace JetBrains.ReSharper.PostfixTemplates.Contexts.CSharp
{
  [Language(typeof(CSharpLanguage))]
  public class CSharpPostfixTemplatesManager : PostfixTemplatesManager<CSharpPostfixTemplateContext>
  {
    public CSharpPostfixTemplatesManager([NotNull] IEnumerable<IPostfixTemplate<CSharpPostfixTemplateContext>> templates)
      : base(templates) { }

    // extra filter for namespaces?
  }
}