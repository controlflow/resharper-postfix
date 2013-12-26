using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;

namespace JetBrains.ReSharper.PostfixTemplates.Templates
{
  [PostfixTemplate(
    templateName: "prop",
    description: "Introduces property for expression",
    example: "Property = expr;")]
  public class IntroducePropertyTemplate : IntroduceMemberTemplateBase
  {
    protected override IntroduceMemberLookupItem CreateItem(PrefixExpressionContext expression,
                                                            IType expressionType, bool isStatic)
    {
      return new IntroducePropertyLookupItem(expression, isStatic);
    }

    private sealed class IntroducePropertyLookupItem : IntroduceMemberLookupItem
    {
      public IntroducePropertyLookupItem([NotNull] PrefixExpressionContext context, bool isStatic)
        : base("prop", context, context.Type, isStatic) { }

      protected override IClassMemberDeclaration CreateMemberDeclaration(CSharpElementFactory factory)
      {
        var declaration = factory.CreatePropertyDeclaration(ExpressionType, "__");
        declaration.SetAccessRights(AccessRights.PUBLIC);
        var getter = factory.CreateAccessorDeclaration(AccessorKind.GETTER, false);
        var setter = factory.CreateAccessorDeclaration(AccessorKind.SETTER, false);

        declaration.AddAccessorDeclarationAfter(getter, null);
        declaration.AddAccessorDeclarationBefore(setter, null);
        declaration.SetStatic(IsStatic);

        return declaration;
      }

      protected override ICSharpTypeMemberDeclaration GetAnchorMember(IList<ICSharpTypeMemberDeclaration> members)
      {
        var anchor = members.LastOrDefault(member =>
                       member.DeclaredElement is IProperty && member.IsStatic == IsStatic) ??
                     members.LastOrDefault(member =>
                       member.DeclaredElement is IField && member.IsStatic == IsStatic);
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