using JetBrains.Annotations;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.Hotspots;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.LiveTemplates;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.Macros;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.Macros.Implementations;
using JetBrains.ReSharper.LiveTemplates;
using JetBrains.ReSharper.PostfixTemplates.LookupItems;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Resolve.Filters;
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
      if (!expressionContext.CanBeStatement) return false;

      var expression = expressionContext.Expression;
      if (context.IsAutoCompletion && !expression.IsPure()) return false;

      if (expressionContext.Type is IArrayType)
      {
        lengthName = "Length";
      }
      else
      {
        if (expressionContext.Type.IsUnknown) return false; // even in force mode

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
          if (!expressionContext.Type.IsInt()) return false;
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
        base.AfterComplete(textControl, statement);

        var condition = (IRelationalExpression) statement.Condition;
        var variable = (ILocalVariableDeclaration) statement.Initializer.Declaration.Declarators[0];
        var iterator = (IPostfixOperatorExpression) statement.Iterators.Expressions[0];

        var suggestVariableName = new MacroCallExpressionNew(new SuggestVariableNameMacroDef());
        var variableNameInfo = new HotspotInfo(
          new TemplateField("name", suggestVariableName, 0),
          variable.NameIdentifier.GetDocumentRange(),
          condition.LeftOperand.GetDocumentRange(),
          iterator.Operand.GetDocumentRange());

        var endRange = new TextRange(textControl.Caret.Offset());
        var session = myTemplatesManager.CreateHotspotSessionAtopExistingText(
          statement.GetSolution(), endRange, textControl,
          LiveTemplatesManager.EscapeAction.LeaveTextAndCaret, variableNameInfo);

        session.Execute();
      }
    }
  }
}