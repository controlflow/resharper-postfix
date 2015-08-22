using System.Collections.Generic;
using JetBrains.Annotations;using JetBrains.ReSharper.Feature.Services.LiveTemplates.Hotspots;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.LiveTemplates;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.Templates;
using JetBrains.ReSharper.PostfixTemplates.CodeCompletion;
using JetBrains.ReSharper.PostfixTemplates.Contexts;
using JetBrains.ReSharper.PostfixTemplates.Contexts.CSharp;
using JetBrains.ReSharper.PostfixTemplates.LookupItems;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Naming.Extentions;
using JetBrains.ReSharper.Psi.Naming.Impl;
using JetBrains.ReSharper.Psi.Pointers;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.TextControl;
using JetBrains.Util;

namespace JetBrains.ReSharper.PostfixTemplates.Templates.CSharp
{
  public abstract class IntroduceMemberTemplateBase : IPostfixTemplate<CSharpPostfixTemplateContext>
  {
    public PostfixTemplateInfo TryCreateInfo(CSharpPostfixTemplateContext context)
    {
      var functionDeclaration = context.ContainingFunction;
      if (functionDeclaration == null) return null;

      var classLikeDeclaration = functionDeclaration.GetContainingNode<IClassLikeDeclaration>();
      if (classLikeDeclaration == null || classLikeDeclaration is IInterfaceDeclaration) return null;

      if (!context.IsPreciseMode || functionDeclaration.DeclaredElement is IConstructor)
      {
        foreach (var expression in context.Expressions)
        {
          if (expression.Type.IsUnknown) continue;
          if (!expression.CanBeStatement) continue;

          // disable over assignments
          if (expression.Expression is IAssignmentExpression) return null;

          var reference = expression.Expression as IReferenceExpression;
          if (reference != null && reference.QualifierExpression == null)
          {
            // filter out other fields and properties
            var target = expression.ReferencedElement;
            if (target == null || target is IField || target is IProperty) continue;
          }

          return new IntroduceMemberPostfixTemplateInfo(
            TemplateName, expression, expression.Type, functionDeclaration.IsStatic);
        }
      }

      return null;
    }

    [NotNull] public abstract string TemplateName { get; }

    protected class IntroduceMemberPostfixTemplateInfo : PostfixTemplateInfo
    {
      [NotNull] public IType ExpressionType { get; private set; }
      public bool IsStatic { get; private set; }

      public IntroduceMemberPostfixTemplateInfo(
        [NotNull] string text, [NotNull] PostfixExpressionContext expression, IType expressionType, bool isStatic)
        : base(text, expression)
      {
        ExpressionType = expressionType;
        IsStatic = isStatic;
      }
    }

    PostfixTemplateBehavior IPostfixTemplate<CSharpPostfixTemplateContext>.CreateBehavior(PostfixTemplateInfo info)
    {
      return CreateBehavior((IntroduceMemberPostfixTemplateInfo) info);
    }

    [NotNull]
    protected abstract PostfixTemplateBehavior CreateBehavior([NotNull] IntroduceMemberPostfixTemplateInfo info);

    protected abstract class CSharpPostfixIntroduceMemberBehaviorBase : CSharpStatementPostfixTemplateBehavior<IExpressionStatement>
    {
      [NotNull] protected readonly IType ExpressionType;
      protected readonly bool IsStatic;

      [NotNull] private readonly LiveTemplatesManager myLiveTemplatesManager;

      [NotNull] private ICollection<string> myMemberNames = EmptyList<string>.InstanceList;
      [CanBeNull] private ITreeNodePointer<IClassMemberDeclaration> myMemberPointer;

      protected CSharpPostfixIntroduceMemberBehaviorBase(
        [NotNull] IntroduceMemberPostfixTemplateInfo info, [NotNull] LiveTemplatesManager liveTemplatesManager) : base(info)
      {
        myLiveTemplatesManager = liveTemplatesManager;

        ExpressionType = info.ExpressionType;
        IsStatic = info.IsStatic;
      }

      protected override IExpressionStatement CreateStatement(CSharpElementFactory factory, ICSharpExpression expression)
      {
        var statement = (IExpressionStatement) factory.CreateStatement("__ = expression;");
        var newDeclaration = CreateMemberDeclaration(factory);
        var assignment = (IAssignmentExpression) statement.Expression;
        assignment.SetSource(expression);

        var psiServices = expression.GetPsiServices();
        var suggestionManager = psiServices.Naming.Suggestion;
        var classDeclaration = expression.GetContainingNode<IClassDeclaration>().NotNull();

        var suggestion = suggestionManager.CreateEmptyCollection(
          PluralityKinds.Unknown, expression.Language, true, expression);

        suggestion.Add(expression, new EntryOptions {
          SubrootPolicy = SubrootPolicy.Decompose,
          PredefinedPrefixPolicy = PredefinedPrefixPolicy.Remove
        });

        suggestion.Prepare(newDeclaration.DeclaredElement, new SuggestionOptions {
          UniqueNameContext = (ITreeNode) classDeclaration.Body ?? classDeclaration
        });

        newDeclaration.SetName(suggestion.FirstName());
        myMemberNames = suggestion.AllNames();

        var memberAnchor = GetAnchorMember(classDeclaration.MemberDeclarations.ToList());
        var newMember = classDeclaration.AddClassMemberDeclarationAfter(
          newDeclaration, (IClassMemberDeclaration) memberAnchor);

        myMemberPointer = newMember.CreateTreeElementPointer();
        return statement;
      }

      [CanBeNull]
      protected abstract ICSharpTypeMemberDeclaration GetAnchorMember([NotNull] IList<ICSharpTypeMemberDeclaration> members);

      [NotNull]
      protected abstract IClassMemberDeclaration CreateMemberDeclaration([NotNull] CSharpElementFactory factory);

      protected override void AfterComplete(ITextControl textControl, IExpressionStatement statement)
      {
        if (myMemberPointer == null) return;

        var memberDeclaration = myMemberPointer.GetTreeNode();
        if (memberDeclaration == null) return;

        var assignment = (IAssignmentExpression) statement.Expression;
        var destination = (IReferenceExpression) assignment.Dest;
        var memberIdentifier = destination.NameIdentifier;

        var hotspotInfo = new HotspotInfo(
          new TemplateField("memberName", new NameSuggestionsExpression(myMemberNames), 0),
          memberIdentifier.GetDocumentRange(), memberDeclaration.GetNameDocumentRange());

        var endRange = statement.GetDocumentRange().EndOffsetRange().TextRange;
        var session = myLiveTemplatesManager.CreateHotspotSessionAtopExistingText(
          statement.GetSolution(), endRange, textControl,
          LiveTemplatesManager.EscapeAction.LeaveTextAndCaret, hotspotInfo);

        session.Execute();
      }
    }
  }
}