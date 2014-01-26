using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.Hotspots;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.LiveTemplates;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.Macros;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.Macros.Implementations;
using JetBrains.ReSharper.Feature.Services.Lookup;
using JetBrains.ReSharper.LiveTemplates;
using JetBrains.ReSharper.PostfixTemplates.LookupItems;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Naming.Extentions;
using JetBrains.ReSharper.Psi.Naming.Impl;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Xaml.Impl;
using JetBrains.TextControl;
using JetBrains.Util;

namespace JetBrains.ReSharper.PostfixTemplates.Templates
{
  [PostfixTemplate(
    templateName: "forEach",
    description: "Iterates over enumerable collection",
    example: "foreach (var x in expr)")]
  public class ForEachLoopTemplate : IPostfixTemplate
  {
    public ILookupItem CreateItem(PostfixTemplateContext context)
    {
      var expressionContext = context.Expressions.LastOrDefault();
      if (expressionContext == null) return null;
      if (!expressionContext.CanBeStatement) return null;

      if (context.IsAutoCompletion && !IsEnumerable(expressionContext)) return null;

      return new ForEachItem(expressionContext);
    }

    private static bool IsEnumerable([NotNull] PrefixExpressionContext context)
    {
      if (!context.Type.IsResolved) return false;

      var predefined = context.Expression.GetPredefinedType();
      var conversionRule = context.Expression.GetTypeConversionRule();
      if (conversionRule.IsImplicitlyConvertibleTo(context.Type, predefined.IEnumerable))
        return true;

      var declaredType = context.Type as IDeclaredType;
      if (declaredType != null && !declaredType.IsUnknown)
      {
        var typeElement = declaredType.GetTypeElement();
        if (typeElement != null && typeElement.IsForeachEnumeratorPatternType())
          return true;
      }

      return false;
    }

    private sealed class ForEachItem : StatementPostfixLookupItem<IForeachStatement>
    {
      [NotNull] private readonly LiveTemplatesManager myTemplatesManager;

      public ForEachItem([NotNull] PrefixExpressionContext context) : base("forEach", context)
      {
        myTemplatesManager = context.PostfixContext.ExecutionContext.LiveTemplatesManager;
      }

      protected override IForeachStatement CreateStatement(CSharpElementFactory factory,
                                                           ICSharpExpression expression)
      {
        var template = "foreach(var x in $0)" + EmbeddedStatementBracesTemplate;
        return (IForeachStatement) factory.CreateStatement(template, expression);
      }

      protected override void AfterComplete(ITextControl textControl, IForeachStatement statement)
      {
        var namesCollection = SuggestIteratorVariableNames(statement);

        var newStatement = PutStatementCaret(textControl, statement);
        if (newStatement == null) return;

        var variableDeclaration = newStatement.IteratorDeclaration;

        var suggestTypeName = new MacroCallExpressionNew(new SuggestVariableTypeMacroDef());
        var typeNameInfo = new HotspotInfo(
          new TemplateField("type", suggestTypeName, 0),
          variableDeclaration.VarKeyword.GetDocumentRange());

        var variableNameInfo = new HotspotInfo(
          new TemplateField("name", new NameSuggestionsExpression(namesCollection), 0),
          variableDeclaration.NameIdentifier.GetDocumentRange());

        var session = myTemplatesManager.CreateHotspotSessionAtopExistingText(
          newStatement.GetSolution(), new TextRange(textControl.Caret.Offset()), textControl,
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
    }
  }
}