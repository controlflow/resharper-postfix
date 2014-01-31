using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.Hotspots;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.LiveTemplates;
using JetBrains.ReSharper.LiveTemplates;
using JetBrains.ReSharper.PostfixTemplates.LookupItems;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Resolve.Filters;
using JetBrains.ReSharper.Psi.Naming.Extentions;
using JetBrains.ReSharper.Psi.Naming.Impl;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.TextControl;
using JetBrains.Util;

namespace JetBrains.ReSharper.PostfixTemplates.Templates
{
  public abstract class ForLoopTemplateBase
  {
    protected bool CreateForItem([NotNull] PostfixTemplateContext context,
                                 [CanBeNull] out string lengthName)
    {
      lengthName = null;

      var expressionContext = context.InnerExpression;
      if (expressionContext == null || !expressionContext.CanBeStatement) return false;

      var expression = expressionContext.Expression;
      if (context.IsAutoCompletion && !expression.IsPure()) return false;

      if (expressionContext.Type is IArrayType)
      {
        lengthName = "Length";
      }
      else
      {
        if (!expressionContext.Type.IsResolved) return false; // even in force mode

        var psiModule = expressionContext.PostfixContext.PsiModule;
        var symbolTable = expressionContext.Type.GetSymbolTable(psiModule);

        var publicProperties = symbolTable.Filter(
          myPropertyFilter, OverriddenFilter.INSTANCE,
          new AccessRightsFilter(new ElementAccessContext(expression)));

        const string countPropertyName = "Count";
        var result = publicProperties.GetResolveResult(countPropertyName);
        var resolveResult = result.DeclaredElement as IProperty;
        if (resolveResult != null)
        {
          if (resolveResult.IsStatic) return false;
          if (!resolveResult.Type.IsInt()) return false;

          lengthName = countPropertyName;
        }
        else
        {
          if (!expressionContext.Type.IsPredefinedIntegralNumeric()) return false;
        }
      }

      return true;
    }

    [NotNull] private readonly DeclaredElementTypeFilter myPropertyFilter =
      new DeclaredElementTypeFilter(ResolveErrorType.NOT_RESOLVED, CLRDeclaredElementType.PROPERTY);

    protected abstract class ForLookupItemBase : StatementPostfixLookupItem<IForStatement>
    {
      [CanBeNull] private readonly string myLengthName;
      [NotNull] private readonly LiveTemplatesManager myTemplatesManager;

      protected ForLookupItemBase([NotNull] string shortcut,
                                  [NotNull] PrefixExpressionContext context,
                                  [CanBeNull] string lengthName)
        : base(shortcut, context)
      {
        var executionContext = context.PostfixContext.ExecutionContext;
        myTemplatesManager = executionContext.LiveTemplatesManager;
        myLengthName = lengthName;
      }

      [CanBeNull] protected string LengthName { get { return myLengthName; } }

      protected override void AfterComplete(ITextControl textControl, IForStatement statement)
      {
        var variableNames = SuggestIteratorVariableNames(statement);

        var newStatement = PutStatementCaret(textControl, statement);
        if (newStatement == null) return;

        var condition = (IRelationalExpression) newStatement.Condition;
        var variable = (ILocalVariableDeclaration) newStatement.Initializer.Declaration.Declarators[0];
        var iterator = (IPostfixOperatorExpression) newStatement.Iterators.Expressions[0];

        var variableNameInfo = new HotspotInfo(
          new TemplateField("name", new NameSuggestionsExpression(variableNames), 0),
          variable.NameIdentifier.GetDocumentRange(),
          condition.LeftOperand.GetDocumentRange(),
          iterator.Operand.GetDocumentRange());

        var endRange = new TextRange(textControl.Caret.Offset());
        var session = myTemplatesManager.CreateHotspotSessionAtopExistingText(
          newStatement.GetSolution(), endRange, textControl,
          LiveTemplatesManager.EscapeAction.LeaveTextAndCaret, variableNameInfo);

        session.Execute();
      }

      [NotNull]
      private static IList<string> SuggestIteratorVariableNames([NotNull] IForStatement statement)
      {
        var declarationMember = statement.Initializer.Declaration.Declarators[0];
        var iteratorDeclaration = (ILocalVariableDeclaration) declarationMember;
        var namingManager = statement.GetPsiServices().Naming;

        var policyProvider = namingManager.Policy.GetPolicyProvider(
          iteratorDeclaration.Language, iteratorDeclaration.GetSourceFile());

        var collection = namingManager.Suggestion.CreateEmptyCollection(
          PluralityKinds.Single, iteratorDeclaration.Language, true, policyProvider);

        var variableType = iteratorDeclaration.DeclaredElement.Type;
        if (variableType.IsResolved)
        {
          collection.Add(variableType, new EntryOptions {
            PluralityKind = PluralityKinds.Single,
            SubrootPolicy = SubrootPolicy.Decompose
          });
        }

        collection.Prepare(iteratorDeclaration.DeclaredElement,
          new SuggestionOptions { UniqueNameContext = statement, DefaultName = "i" });

        return collection.AllNames();
      }
    }
  }
}