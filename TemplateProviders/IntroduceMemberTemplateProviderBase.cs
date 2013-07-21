using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.ReSharper.ControlFlow.PostfixCompletion.LookupItems;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.Hotspots;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.LiveTemplates;
using JetBrains.ReSharper.Feature.Services.Lookup;
using JetBrains.ReSharper.LiveTemplates;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Naming.Extentions;
using JetBrains.ReSharper.Psi.Naming.Impl;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.TextControl;
using JetBrains.Util;
#if RESHARPER8
using JetBrains.ReSharper.Psi.Modules;
#endif

namespace JetBrains.ReSharper.ControlFlow.PostfixCompletion.TemplateProviders
{
  public abstract class IntroduceMemberTemplateProviderBase : IPostfixTemplateProvider
  {
    public void CreateItems(
      PostfixTemplateAcceptanceContext context, ICollection<ILookupItem> consumer)
    {
      var functionDeclaration = context.ContainingFunction;
      if (functionDeclaration == null) return;

      var classDeclaration = functionDeclaration.GetContainingNode<IClassDeclaration>();
      if (classDeclaration == null) return;

      if (context.ForceMode || functionDeclaration.DeclaredElement is IConstructor)
      {
        foreach (var expression in context.PossibleExpressions)
        {
          if (expression.Type.IsUnknown) continue;
          if (!expression.CanBeStatement) continue;

          var reference = expression.Expression as IReferenceExpression;
          if (reference != null && reference.QualifierExpression == null)
          {
            // filter out other fields and properties
            var target = expression.ReferencedElement;
            if (target == null || target is IField || target is IProperty) continue;
          }

          var lookupItem = CreateLookupItem(
            expression, expression.Type, functionDeclaration.IsStatic);
          consumer.Add(lookupItem);
          break;
        }
      }
    }

    protected abstract IntroduceMemberLookupItem CreateLookupItem(
      [NotNull] PrefixExpressionContext expression, [NotNull] IType expressionType, bool isStatic);

    protected abstract class IntroduceMemberLookupItem
      : StatementPostfixLookupItem<IExpressionStatement>
    {
      [NotNull] protected readonly IType ExpressionType;
      protected readonly bool IsStatic;

      [NotNull] private ICollection<string> myMemberNames;
      [CanBeNull] private IDeclaration myMemberDeclaration;

      protected IntroduceMemberLookupItem(
        [NotNull] string shortcut, [NotNull] PrefixExpressionContext context,
        [NotNull] IType expressionType, bool isStatic)
        : base(shortcut, context)
      {
        IsStatic = isStatic;
        ExpressionType = expressionType;
        myMemberNames = EmptyList<string>.InstanceList;
      }

      protected override IExpressionStatement CreateStatement(
        IPsiModule psiModule, CSharpElementFactory factory)
      {
        return (IExpressionStatement) factory.CreateStatement("__ = expression;");
      }

      protected override void PlaceExpression(
        IExpressionStatement statement, ICSharpExpression expression, CSharpElementFactory factory)
      {
        var classDeclaration = statement.GetContainingNode<IClassDeclaration>().NotNull();
        var anchor = GetAnchorMember(classDeclaration.MemberDeclarations);

        var newDeclaration = CreateMemberDeclaration(factory);
        var newMember = classDeclaration.AddClassMemberDeclarationAfter(
          newDeclaration, (IClassMemberDeclaration) anchor);

        var assignment = (IAssignmentExpression) statement.Expression;
        assignment.SetSource(expression);

        var suggestionManager = statement.GetPsiServices().Naming.Suggestion;
        var collection = suggestionManager.CreateEmptyCollection(
          PluralityKinds.Unknown, classDeclaration.Language, true, statement);

        collection.Add(assignment.Source, new EntryOptions());
        collection.Prepare(newMember.DeclaredElement,
          new SuggestionOptions { UniqueNameContext = classDeclaration });

        newMember.SetName(collection.FirstName());
        myMemberNames = collection.AllNames();
        myMemberDeclaration = newMember;
      }

      [CanBeNull] protected abstract ICSharpTypeMemberDeclaration GetAnchorMember(
        TreeNodeCollection<ICSharpTypeMemberDeclaration> members);

      [NotNull] protected abstract IClassMemberDeclaration
        CreateMemberDeclaration([NotNull] CSharpElementFactory factory);

      protected override void AfterComplete(
        ITextControl textControl, Suffix suffix,
        IExpressionStatement newStatement, int? caretPosition)
      {
        // note: supress suffix, yeah
        if (newStatement == null || myMemberDeclaration == null) return;

        var assignment = (IAssignmentExpression) newStatement.Expression;
        var memberIdentifier = ((IReferenceExpression) assignment.Dest).NameIdentifier;
        var suggestionsExpression = new NameSuggestionsExpression(myMemberNames);

        var hotspotInfo = new HotspotInfo(
          new TemplateField("memberName", suggestionsExpression, 0),
#if RESHARPER7
          memberIdentifier.GetDocumentRange().TextRange,
          myMemberDeclaration.GetNameDocumentRange().TextRange
#else
          memberIdentifier.GetDocumentRange(),
          myMemberDeclaration.GetNameDocumentRange()
#endif
          );

        var endSelectionRange = newStatement.GetDocumentRange().EndOffsetRange().TextRange;
        var session = LiveTemplatesManager.Instance.CreateHotspotSessionAtopExistingText(
          newStatement.GetSolution(), endSelectionRange, textControl,
          LiveTemplatesManager.EscapeAction.LeaveTextAndCaret, new[] { hotspotInfo });

        session.Execute();
      }
    }
  }
}