using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.ReSharper.PostfixTemplates.LookupItems;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;

namespace JetBrains.ReSharper.PostfixTemplates.Templates.CSharp
{
  [PostfixTemplate(
    templateName: "field",
    description: "Introduces field for expression",
    example: "_field = expr;")]
  public class IntroduceFieldTemplate : IntroduceMemberTemplateBase
  {
    public override string TemplateName { get { return "field"; } }

    protected override PostfixTemplateBehavior CreateBehavior(IntroduceMemberPostfixTemplateInfo info)
    {
      return new CSharpPostfixIntroduceFieldBehaviorBase(info);
    }

    private sealed class CSharpPostfixIntroduceFieldBehaviorBase : CSharpPostfixIntroduceMemberBehaviorBase
    {
      public CSharpPostfixIntroduceFieldBehaviorBase([NotNull] IntroduceMemberPostfixTemplateInfo info) : base(info) { }

      protected override IClassMemberDeclaration CreateMemberDeclaration(CSharpElementFactory factory)
      {
        var declaration = factory.CreateFieldDeclaration(ExpressionType, "__");
        declaration.SetStatic(IsStatic);

        return declaration;
      }

      protected override ICSharpTypeMemberDeclaration GetAnchorMember(IList<ICSharpTypeMemberDeclaration> members)
      {
        return members.LastOrDefault(member => member.DeclaredElement is IField && member.IsStatic == IsStatic);
      }
    }
  }
}