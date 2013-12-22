using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;

namespace JetBrains.ReSharper.PostfixTemplates.Templates
{
  [PostfixTemplate(
    templateName: "field",
    description: "Introduces field for expression",
    example: "_field = expr;")]
  public class IntroduceFieldTemplate : IntroduceMemberTemplateBase
  {
    protected override IntroduceMemberLookupItem CreateItem(
      PrefixExpressionContext expression, IType expressionType, bool isStatic)
    {
      return new IntroduceFieldLookupItem(expression, expressionType, isStatic);
    }

    private sealed class IntroduceFieldLookupItem : IntroduceMemberLookupItem
    {
      public IntroduceFieldLookupItem(
        [NotNull] PrefixExpressionContext context,
        [NotNull] IType expressionType, bool isStatic)
        : base("field", context, expressionType, isStatic) { }

      protected override IClassMemberDeclaration CreateMemberDeclaration(CSharpElementFactory factory)
      {
        var declaration = factory.CreateFieldDeclaration(ExpressionType, "__");
        declaration.SetStatic(IsStatic);

        return declaration;
      }

      protected override ICSharpTypeMemberDeclaration GetAnchorMember(IList<ICSharpTypeMemberDeclaration> members)
      {
        return members.LastOrDefault(member =>
          member.DeclaredElement is IField && member.IsStatic == IsStatic);
      }
    }
  }
}