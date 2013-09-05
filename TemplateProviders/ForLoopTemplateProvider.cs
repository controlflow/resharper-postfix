using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.ReSharper.ControlFlow.PostfixCompletion.LookupItems;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.Hotspots;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.LiveTemplates;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.Macros;
using JetBrains.ReSharper.Feature.Services.Lookup;
using JetBrains.ReSharper.LiveTemplates;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Resolve.Filters;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.TextControl;
using JetBrains.Util;
#if RESHARPER8
using JetBrains.ReSharper.Feature.Services.LiveTemplates.Macros.Implementations;
#endif

namespace JetBrains.ReSharper.ControlFlow.PostfixCompletion.TemplateProviders
{
  [PostfixTemplateProvider(
    templateNames: new[]{"for", "forr"},
    description: "Iterates over collection/number of times",
    example: "for (var i = 0; i < expr.Length; i++)")]
  public class ForLoopTemplateProvider : IPostfixTemplateProvider
  {
    [NotNull] private readonly DeclaredElementTypeFilter myPropertyFilter =
      new DeclaredElementTypeFilter(ResolveErrorType.NOT_RESOLVED, CLRDeclaredElementType.PROPERTY);

    public void CreateItems(PostfixTemplateAcceptanceContext context, ICollection<ILookupItem> consumer)
    {
      var exprContext = context.InnerExpression;
      if (!exprContext.CanBeStatement) return;

      var expression = exprContext.Expression;
      //if (!context.ForceMode && BooleanExpressionProviderBase.IsBooleanExpression(expression))
      //  return;

      if (context.ForceMode || expression.IsPure())
      {
        string lengthPropertyName;
        if (exprContext.Type is IArrayType)
        {
          lengthPropertyName = "Length";
        }
        else
        {
          if (exprContext.Type.IsUnknown) return; // even in force mode

          var table = exprContext.Type.GetSymbolTable(context.PsiModule);
          var publicProperties = table.Filter(
            myPropertyFilter, OverriddenFilter.INSTANCE,
            new AccessRightsFilter(new ElementAccessContext(expression)));

          var result = publicProperties.GetResolveResult("Count");
          var resolveResult = result.DeclaredElement as IProperty;
          if (resolveResult != null)
          {
            if (resolveResult.IsStatic) return;
            if (!resolveResult.Type.IsInt()) return;
            lengthPropertyName = "Count";
          }
          else
          {
            if (!exprContext.Type.IsInt()) return;
            lengthPropertyName = null;
          }
        }

        consumer.Add(new ForLookupItem(exprContext, lengthPropertyName));
        consumer.Add(new ReverseForLookupItem(exprContext, lengthPropertyName));
      }
    }

    private abstract class ForLookupItemBase : KeywordStatementPostfixLookupItem<IForStatement>
    {
      protected ForLookupItemBase([NotNull] string shortcut,
        [NotNull] PrefixExpressionContext context, [CanBeNull] string lengthPropertyName)
        : base(shortcut, context)
      {
        LengthPropertyName = lengthPropertyName;
      }

      [CanBeNull] protected string LengthPropertyName { get; private set; }

      protected override void AfterComplete(
        ITextControl textControl, Suffix suffix, IForStatement statement, int? caretPosition)
      {
        if (caretPosition == null) return;

        var condition = (IRelationalExpression) statement.Condition;
        var variable = (ILocalVariableDeclaration) statement.Initializer.Declaration.Declarators[0];
        var iterator = (IPostfixOperatorExpression) statement.Iterators.Expressions[0];

#if RESHARPER7
        var nameExpression = new MacroCallExpression(new SuggestVariableNameMacro());
#else
        var nameExpression = new MacroCallExpressionNew(new SuggestVariableNameMacroDef());
#endif

        var nameSpot = new HotspotInfo(
          new TemplateField("name", nameExpression, 0),
          variable.NameIdentifier.GetDocumentRange().GetHotspotRange(),
          condition.LeftOperand.GetDocumentRange().GetHotspotRange(),
          iterator.Operand.GetDocumentRange().GetHotspotRange());

        var session = LiveTemplatesManager.Instance.CreateHotspotSessionAtopExistingText(
          statement.GetSolution(), new TextRange(caretPosition.Value), textControl,
          LiveTemplatesManager.EscapeAction.LeaveTextAndCaret, new[] { nameSpot });

        session.Execute();
      }
    }

    private sealed class ForLookupItem : ForLookupItemBase
    {
      public ForLookupItem(
        [NotNull] PrefixExpressionContext context, [CanBeNull] string lengthPropertyName)
        : base("for", context, lengthPropertyName) { }

      protected override string Template { get { return "for(var x=0;x<expr;x++)"; } }

      protected override void PlaceExpression(
        IForStatement forStatement, ICSharpExpression expression, CSharpElementFactory factory)
      {
        var condition = (IRelationalExpression) forStatement.Condition;
        if (LengthPropertyName == null)
        {
          condition.RightOperand.ReplaceBy(expression);
        }
        else
        {
          var lengthAccess = factory.CreateReferenceExpression("expr.$0", LengthPropertyName);
          lengthAccess = condition.RightOperand.ReplaceBy(lengthAccess);
          lengthAccess.QualifierExpression.NotNull().ReplaceBy(expression);
        }
      }
    }

    private sealed class ReverseForLookupItem : ForLookupItemBase
    {
      public ReverseForLookupItem(
        [NotNull] PrefixExpressionContext context, [CanBeNull] string lengthPropertyName)
        : base("forR", context, lengthPropertyName) { }

      protected override string Template { get { return "for(var x=expr;x>=0;x--)"; } }

      protected override void PlaceExpression(
        IForStatement forStatement, ICSharpExpression expression, CSharpElementFactory factory)
      {
        var variable = (ILocalVariableDeclaration) forStatement.Initializer.Declaration.Declarators[0];
        var initializer = (IExpressionInitializer) variable.Initial;

        if (LengthPropertyName == null)
        {
          var value = initializer.Value.ReplaceBy(expression);
          value.ReplaceBy(factory.CreateExpression("$0 - 1", value));
        }
        else
        {
          var lengthAccess = factory.CreateReferenceExpression("expr.$0", LengthPropertyName);
          lengthAccess = initializer.Value.ReplaceBy(lengthAccess);
          lengthAccess.QualifierExpression.NotNull().ReplaceBy(expression);
          lengthAccess.ReplaceBy(factory.CreateExpression("$0 - 1", lengthAccess));
        }
      }
    }
  }
}