using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.Application.Settings;
using JetBrains.DocumentModel;
using JetBrains.ReSharper.Feature.Services.LinqTools;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.Hotspots;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.LiveTemplates;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.Macros;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.Macros.Implementations;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.Templates;
using JetBrains.ReSharper.PostfixTemplates.CodeCompletion;
using JetBrains.ReSharper.PostfixTemplates.Contexts.CSharp;
using JetBrains.ReSharper.PostfixTemplates.LookupItems;
using JetBrains.ReSharper.PostfixTemplates.Settings;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Impl;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Naming.Extentions;
using JetBrains.ReSharper.Psi.Naming.Impl;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.TextControl;
using JetBrains.Util;

namespace JetBrains.ReSharper.PostfixTemplates.Templates.CSharp
{
  // todo: hide from auto for string type?
  // todo: apply var code style in 9.0
  // todo: .from template?
  // todo: detect type by indexer return type, like F# do?

  [PostfixTemplate(
    templateName: "forEach",
    description: "Iterates over enumerable collection",
    example: "foreach (var x in expr)")]
  public class ForEachLoopTemplate : IPostfixTemplate<CSharpPostfixTemplateContext>
  {
    public PostfixTemplateInfo TryCreateInfo(CSharpPostfixTemplateContext context)
    {
      var expressionContext = context.Expressions.LastOrDefault();
      if (expressionContext == null) return null;

      if (context.IsPreciseMode)
      {
        if (!expressionContext.CanBeStatement) return null;
        if (!IsEnumerable(expressionContext)) return null;
      }

      var target = expressionContext.CanBeStatement ? PostfixTemplateTarget.Statement : PostfixTemplateTarget.Expression;
      return new PostfixTemplateInfo("foreach", expressionContext, target: target);
    }

    private static bool IsEnumerable([NotNull] CSharpPostfixExpressionContext context)
    {
      if (!context.Type.IsResolved) return false;

      var predefined = context.Expression.GetPredefinedType();
      var conversionRule = context.Expression.GetTypeConversionRule();
      if (conversionRule.IsImplicitlyConvertibleTo(context.Type, predefined.IEnumerable)) return true;

      var declaredType = context.Type as IDeclaredType;
      if (declaredType == null || declaredType.IsUnknown) return false;

      var typeElement = declaredType.GetTypeElement();
      return typeElement != null && typeElement.IsForeachEnumeratorPatternType();
    }

    public PostfixTemplateBehavior CreateBehavior(PostfixTemplateInfo info)
    {
      if (info.Target == PostfixTemplateTarget.Statement)
        return new CSharpPostfixForEachStatementBehavior(info);

      return new CSharpPostfixForEachExpressionBehavior(info);
    }

    private sealed class CSharpPostfixForEachStatementBehavior : CSharpStatementPostfixTemplateBehavior<IForeachStatement>
    {
      public CSharpPostfixForEachStatementBehavior([NotNull] PostfixTemplateInfo info) : base(info) { }

      //public override MatchingResult Match(PrefixMatcher prefixMatcher, ITextControl textControl)
      //{
      //  var coolMatcher = prefixMatcher.Factory.CreatePrefixMatcher(
      //    HackPrefix(prefixMatcher.Prefix), prefixMatcher.IdentifierMatchingStyle);
      //
      //  return base.Match(coolMatcher, textControl);
      //}

      protected override IForeachStatement CreateStatement(CSharpElementFactory factory, ICSharpExpression expression)
      {
        var template = "foreach(var x in $0)" + EmbeddedStatementBracesTemplate;
        return (IForeachStatement) factory.CreateStatement(template, expression);
      }

      protected override void AfterComplete(ITextControl textControl, IForeachStatement statement)
      {
        var namesCollection = SuggestIteratorVariableNames(statement);

        var newStatement = PutStatementCaret(textControl, statement);
        if (newStatement == null) return;

        var liveTemplatesManager = Info.ExecutionContext.LiveTemplatesManager;
        ApplyRenameHotspots(liveTemplatesManager, textControl, newStatement, namesCollection);
      }
    }

    private sealed class CSharpPostfixForEachExpressionBehavior : CSharpExpressionPostfixTemplateBehavior<ICSharpExpression>
    {
      private readonly bool myUseBraces;

      public CSharpPostfixForEachExpressionBehavior([NotNull] PostfixTemplateInfo info) : base(info)
      {
        myUseBraces = info.ExecutionContext.SettingsStore.GetValue(PostfixTemplatesSettingsAccessor.BracesForStatements);
      }

      //public override MatchingResult Match(PrefixMatcher prefixMatcher, ITextControl textControl)
      //{
      //  var coolMatcher = prefixMatcher.Factory.CreatePrefixMatcher(
      //    HackPrefix(prefixMatcher.Prefix), prefixMatcher.IdentifierMatchingStyle);
      //
      //  return base.Match(coolMatcher, textControl);
      //}

      protected override ICSharpExpression CreateExpression(CSharpElementFactory factory, ICSharpExpression expression)
      {
        return expression;
      }

      protected override void AfterComplete(ITextControl textControl, ICSharpExpression expression)
      {
        IReferenceExpression iteratorReference = null;
        var psiServices = expression.GetPsiServices();

        var resultStatement = psiServices.DoTransaction(ExpandCommandName, () =>
        {
          var containingStatement = expression.GetContainingStatement();
          if (containingStatement == null) return null;

          var factory = CSharpElementFactory.GetInstance(expression);
          var newStatement = factory.CreateStatement("foreach(var x in $0){}", expression);

          var fakeReference = expression.ReplaceBy(factory.CreateReferenceExpression("__"));
          var nodeMarker = new TreeNodeMarker<IReferenceExpression>(fakeReference);

          var statementCopy = containingStatement.Copy();
          var foreachStatement = (IForeachStatement) containingStatement.ReplaceBy(newStatement);
          var bodyBlock = (IBlock) foreachStatement.Body;

          statementCopy = myUseBraces
            ? bodyBlock.AddStatementAfter(statementCopy, null)
            : bodyBlock.ReplaceBy(statementCopy);

          iteratorReference = nodeMarker.FindMarkedNode(statementCopy);
          if (iteratorReference != null)
            iteratorReference.Reference.BindTo(foreachStatement.IteratorDeclaration.DeclaredElement);

          return foreachStatement;
        });

        if (resultStatement == null) return;

        var namesCollection = SuggestIteratorVariableNames(resultStatement);
        var liveTemplatesManager = Info.ExecutionContext.LiveTemplatesManager;

        ApplyRenameHotspots(liveTemplatesManager, textControl, resultStatement, namesCollection, iteratorReference);
      }
    }

    private static void ApplyRenameHotspots(
      [NotNull] LiveTemplatesManager liveTemplatesManager, [NotNull] ITextControl textControl, [NotNull] IForeachStatement statement,
      [NotNull] IList<string> namesCollection, [CanBeNull] IReferenceExpression extraReference = null)
    {
      var variableDeclaration = statement.IteratorDeclaration;
      var endSelectionRange = new TextRange(textControl.Caret.Offset());

      var suggestTypeName = new MacroCallExpressionNew(new SuggestVariableTypeMacroDef());
      var typeNameInfo = new HotspotInfo(
        new TemplateField("type", suggestTypeName, 0),
        variableDeclaration.VarKeyword.GetDocumentRange());

      var nameRanges = new LocalList<DocumentRange>();
      nameRanges.Add(variableDeclaration.NameIdentifier.GetDocumentRange());

      if (extraReference != null)
      {
        var documentRange = extraReference.GetDocumentRange();
        nameRanges.Add(documentRange);
        endSelectionRange = new TextRange(documentRange.TextRange.EndOffset);
      }

      var variableNameInfo = new HotspotInfo(
        new TemplateField("name", new NameSuggestionsExpression(namesCollection), 0), nameRanges.ToArray());

      var session = liveTemplatesManager.CreateHotspotSessionAtopExistingText(
        statement.GetSolution(), endSelectionRange, textControl,
        LiveTemplatesManager.EscapeAction.LeaveTextAndCaret, typeNameInfo, variableNameInfo);

      session.Execute();
    }

    [NotNull]
    private static IList<string> SuggestIteratorVariableNames([NotNull] IForeachStatement statement)
    {
      var iteratorDeclaration = statement.IteratorDeclaration;
      var namingManager = statement.GetPsiServices().Naming;

      var policyProvider = namingManager.Policy.GetPolicyProvider(
        iteratorDeclaration.Language, iteratorDeclaration.GetSourceFile());

      var collection = namingManager.Suggestion.CreateEmptyCollection(
        PluralityKinds.Single, iteratorDeclaration.Language, true, policyProvider);

      var expression = statement.Collection;
      if (expression != null)
      {
        collection.Add(expression, new EntryOptions {
          PluralityKind = PluralityKinds.Plural,
          SubrootPolicy = SubrootPolicy.Decompose,
          PredefinedPrefixPolicy = PredefinedPrefixPolicy.Remove
        });
      }

      var variableType = iteratorDeclaration.DeclaredElement.Type;
      if (variableType.IsResolved)
      {
        collection.Add(variableType, new EntryOptions {
          PluralityKind = PluralityKinds.Single,
          SubrootPolicy = SubrootPolicy.Decompose
        });
      }

      collection.Prepare(iteratorDeclaration.DeclaredElement,
        new SuggestionOptions {UniqueNameContext = statement});

      return collection.AllNames();
    }

    // todo: eww
    // todo: how to port this into 9.0? 'for' is really rare, I think
    [NotNull] private static string HackPrefix([NotNull] string prefix)
    {
      if (prefix.Length > 0 && prefix.Length <= 3 &&
          prefix.Equals("for".Substring(0, prefix.Length), StringComparison.OrdinalIgnoreCase))
      {
        return prefix + "Each";
      }

      return prefix;
    }
  }
}