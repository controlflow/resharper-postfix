using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.LiveTemplates;
using JetBrains.ReSharper.PostfixTemplates.CodeCompletion;
using JetBrains.ReSharper.PostfixTemplates.LookupItems;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;

// TODO: C# 6.0 get-only auto-properties support
// TODO: emit private setter in C# 5.0

namespace JetBrains.ReSharper.PostfixTemplates.Templates.CSharp
{
  [PostfixTemplate(
    templateName: "prop",
    description: "Introduces property for expression",
    example: "Property = expr;")]
  public class IntroducePropertyTemplate : IntroduceMemberTemplateBase
  {
    [NotNull] private readonly LiveTemplatesManager myLiveTemplatesManager;

    public IntroducePropertyTemplate([NotNull] LiveTemplatesManager liveTemplatesManager)
    {
      myLiveTemplatesManager = liveTemplatesManager;
    }

    public override string TemplateName { get { return "prop"; } }

    protected override PostfixTemplateBehavior CreateBehavior(IntroduceMemberPostfixTemplateInfo info)
    {
      return new CSharpPostfixIntroducePropertyBehaviorBase(info, myLiveTemplatesManager);
    }

    private sealed class CSharpPostfixIntroducePropertyBehaviorBase : CSharpPostfixIntroduceMemberBehaviorBase
    {
      public CSharpPostfixIntroducePropertyBehaviorBase(
        [NotNull] IntroduceMemberPostfixTemplateInfo info, [NotNull] LiveTemplatesManager liveTemplatesManager)
        : base(info, liveTemplatesManager) { }

      protected override IClassMemberDeclaration CreateMemberDeclaration(CSharpElementFactory factory)
      {
        var declaration = factory.CreatePropertyDeclaration(ExpressionType, "__");
        declaration.SetAccessRights(AccessRights.PUBLIC);
        var getter = factory.CreateAccessorDeclaration(AccessorKind.GETTER, withBody: false);
        var setter = factory.CreateAccessorDeclaration(AccessorKind.SETTER, withBody: false);

        declaration.AddAccessorDeclarationAfter(getter, null);
        declaration.AddAccessorDeclarationBefore(setter, null);
        declaration.SetStatic(IsStatic);

        return declaration;
      }

      protected override ICSharpTypeMemberDeclaration GetAnchorMember(IList<ICSharpTypeMemberDeclaration> members)
      {
        var anchor = members.LastOrDefault(member => member.DeclaredElement is IProperty && member.IsStatic == IsStatic) ??
                     members.LastOrDefault(member => member.DeclaredElement is IField && member.IsStatic == IsStatic);
        if (anchor == null && IsStatic)
        {
          return members.LastOrDefault(m => m.DeclaredElement is IProperty) ??
                 members.LastOrDefault(m => m.DeclaredElement is IField);
        }

        return anchor;
      }
    }
  }
}