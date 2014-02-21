using System.Collections.Generic;
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
using JetBrains.ReSharper.Psi.CSharp.Impl;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Naming.Extentions;
using JetBrains.ReSharper.Psi.Naming.Impl;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.TextControl;
using JetBrains.Util;

namespace JetBrains.ReSharper.PostfixTemplates.Templates
{
  [PostfixTemplate(
    templateName: "using",
    description: "Wraps resource with using statement",
    example: "using (expr)")]
  public class UsingStatementTemplate : IPostfixTemplate
  {
    public ILookupItem CreateItem(PostfixTemplateContext context)
    {
      var expressionContext = context.OuterExpression;
      if (expressionContext == null || !expressionContext.CanBeStatement) return null;

      var expression = expressionContext.Expression;
      var shouldCreateVariable = true;

      var disposableType = expression.GetPredefinedType().IDisposable;
      if (!disposableType.IsResolved) return null;

      var conversionRule = expression.GetTypeConversionRule();
      var expressionType = expressionContext.ExpressionType;

      if (context.IsAutoCompletion)
      {
        if (!expressionType.IsResolved)
          return null;
        if (!expressionType.IsImplicitlyConvertibleTo(disposableType, conversionRule))
          return null;
      }

      if (expressionType.IsResolved && disposableType.Equals(expressionType.ToIType()))
      {
        shouldCreateVariable = false;
      }

      // check expression is local variable reference
      var resourceVariable = expressionContext.ReferencedElement as ITypeOwner;
      if (resourceVariable is ILocalVariable || resourceVariable is IParameter)
      {
        shouldCreateVariable = false;
      }

      for (ITreeNode node = expression; context.IsAutoCompletion;)
      {
        // inspect containing using statements
        var usingStatement = node.GetContainingNode<IUsingStatement>();
        if (usingStatement == null) break;

        // check if expressions is variable declared with using statement
        var declaration = usingStatement.Declaration;
        if (resourceVariable is ILocalVariable && declaration != null)
        {
          foreach (var member in declaration.DeclaratorsEnumerable)
            if (Equals(member.DeclaredElement, resourceVariable)) return null;
        }

        // check expression is already in using statement expression
        if (declaration == null)
        {
          foreach (var expr in usingStatement.ExpressionsEnumerable)
            if (MiscUtil.AreExpressionsEquivalent(expr, expression)) return null;
        }

        node = usingStatement;
      }

      return new UsingItem(expressionContext, shouldCreateVariable);
    }

    private sealed class UsingItem : StatementPostfixLookupItem<IUsingStatement>
    {
      [NotNull] private readonly LiveTemplatesManager myTemplatesManager;
      private readonly bool myShouldCreateVariable;

      public UsingItem([NotNull] PrefixExpressionContext context, bool shouldCreateVariable)
        : base("using", context)
      {
        myTemplatesManager = context.PostfixContext.ExecutionContext.LiveTemplatesManager;
        myShouldCreateVariable = shouldCreateVariable;
      }

      protected override IUsingStatement CreateStatement(CSharpElementFactory factory,
                                                         ICSharpExpression expression)
      {
        var template = (myShouldCreateVariable ? "using(T x=$0)" : "using($0)");
        return (IUsingStatement) factory.CreateStatement(
          template + EmbeddedStatementBracesTemplate, expression);
      }

      protected override void AfterComplete(ITextControl textControl, IUsingStatement statement)
      {
        var variableNames = SuggestResourceVariableNames(statement);

        var newStatement = PutStatementCaret(textControl, statement);
        if (newStatement == null) return;

        var resourceDeclaration = newStatement.Declaration;
        if (resourceDeclaration == null) return;

        var declaration = (ILocalVariableDeclaration) resourceDeclaration.Declarators[0];
        var typeExpression = new MacroCallExpressionNew(new SuggestVariableTypeMacroDef());
        var nameExpression = new NameSuggestionsExpression(variableNames);

        var typeSpot = new HotspotInfo(
          new TemplateField("type", typeExpression, 0),
          declaration.TypeUsage.GetDocumentRange());

        var nameSpot = new HotspotInfo(
          new TemplateField("name", nameExpression, 0),
          declaration.NameIdentifier.GetDocumentRange());

        var session = myTemplatesManager.CreateHotspotSessionAtopExistingText(
          newStatement.GetSolution(), new TextRange(textControl.Caret.Offset()), textControl,
          LiveTemplatesManager.EscapeAction.LeaveTextAndCaret, typeSpot, nameSpot);

        session.Execute();
      }

      [NotNull]
      private static IList<string> SuggestResourceVariableNames([NotNull] IUsingStatement statement)
      {
        var variableDeclaration = statement.Declaration;
        if (variableDeclaration == null) return EmptyList<string>.InstanceList;

        var localVariable = statement.Declaration.Declarators[0] as ILocalVariableDeclaration;
        if (localVariable == null) return EmptyList<string>.InstanceList;

        var namingManager = statement.GetPsiServices().Naming;

        var policyProvider = namingManager.Policy.GetPolicyProvider(
          variableDeclaration.Language, variableDeclaration.GetSourceFile());

        var collection = namingManager.Suggestion.CreateEmptyCollection(
          PluralityKinds.Single, variableDeclaration.Language, true, policyProvider);

        var initializer = localVariable.Initial as IExpressionInitializer;
        if (initializer != null)
        {
          collection.Add(initializer.Value, new EntryOptions {
            SubrootPolicy = SubrootPolicy.Decompose,
            PredefinedPrefixPolicy = PredefinedPrefixPolicy.Remove
          });

          var variableType = initializer.Value.Type();
          if (variableType.IsResolved)
          {
            collection.Add(variableType, new EntryOptions {
              SubrootPolicy = SubrootPolicy.Decompose
            });
          }
        }

        var suggestionOptions = new SuggestionOptions {UniqueNameContext = statement};
        collection.Prepare(localVariable.DeclaredElement, suggestionOptions);

        return collection.AllNames();
      }
    }
  }
}