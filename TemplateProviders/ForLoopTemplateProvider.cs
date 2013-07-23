using System.Collections.Generic;
using System.Linq;
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
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Xaml.Impl;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.TextControl;
using JetBrains.Util;
#if RESHARPER8
using JetBrains.ReSharper.Feature.Services.LiveTemplates.Macros.Implementations;
#endif

namespace JetBrains.ReSharper.ControlFlow.PostfixCompletion.TemplateProviders
{
  [PostfixTemplateProvider(new[]{"for", "forr"}, "Iterating over collections with length")]
  public class ForLoopTemplateProvider : IPostfixTemplateProvider
  {
    public void CreateItems(PostfixTemplateAcceptanceContext context, ICollection<ILookupItem> consumer)
    {
      var exprContext = context.PossibleExpressions.FirstOrDefault();
      if (exprContext == null || !exprContext.CanBeStatement) return;

      var expression = exprContext.Expression;
      if (context.ForceMode || expression.IsPure())
      {
        string lengthPropertyName;
        if (exprContext.Type is IArrayType)
        {
          lengthPropertyName = "Length";
        }
        else
        {
          var predefined = expression.GetPredefinedType();
          var rule = expression.GetTypeConversionRule();
          if (!rule.IsImplicitlyConvertibleTo(exprContext.Type, predefined.GenericICollection))
            return;

          lengthPropertyName = "Count";
        }

        consumer.Add(new ForLookupItem(exprContext, lengthPropertyName));
        consumer.Add(new ReverseForLookupItem(exprContext, lengthPropertyName));
      }
    }

    private abstract class ForLookupItemBase : KeywordStatementPostfixLookupItem<IForStatement>
    {
      protected ForLookupItemBase([NotNull] string shortcut,
        [NotNull] PrefixExpressionContext context, [NotNull] string lengthPropertyName)
        : base(shortcut, context)
      {
        LengthPropertyName = lengthPropertyName;
      }

      [NotNull] protected string LengthPropertyName { get; private set; }

      protected override void AfterComplete(
        ITextControl textControl, Suffix suffix, IForStatement newStatement, int? caretPosition)
      {
        if (newStatement == null || caretPosition == null) return;

        var condition = (IRelationalExpression) newStatement.Condition;
        var variable = (ILocalVariableDeclaration) newStatement.Initializer.Declaration.Declarators[0];
        var iterator = (IPostfixOperatorExpression) newStatement.Iterators.Expressions[0];

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
          newStatement.GetSolution(), new TextRange(caretPosition.Value), textControl,
          LiveTemplatesManager.EscapeAction.LeaveTextAndCaret, new[] { nameSpot });

        session.Execute();
      }
    }

    private sealed class ForLookupItem : ForLookupItemBase
    {
      public ForLookupItem(
        [NotNull] PrefixExpressionContext context, [NotNull] string lengthPropertyName)
        : base("for", context, lengthPropertyName) { }

      protected override string Template { get { return "for(var x=0;x<expr;x++)"; } }

      protected override void PlaceExpression(
        IForStatement forStatement, ICSharpExpression expression, CSharpElementFactory factory)
      {
        var condition = (IRelationalExpression) forStatement.Condition;
        var lengthAccess = factory.CreateReferenceExpression("expr.$0", LengthPropertyName);
        lengthAccess = condition.RightOperand.ReplaceBy(lengthAccess);
        lengthAccess.QualifierExpression.NotNull().ReplaceBy(expression);
      }
    }

    private sealed class ReverseForLookupItem : ForLookupItemBase
    {
      public ReverseForLookupItem(
        [NotNull] PrefixExpressionContext context, [NotNull] string lengthPropertyName)
        : base("forR", context, lengthPropertyName) { }

      protected override string Template { get { return "for(var x=expr;x>=0;x--)"; } }

      protected override void PlaceExpression(
        IForStatement forStatement, ICSharpExpression expression, CSharpElementFactory factory)
      {
        var variable = (ILocalVariableDeclaration) forStatement.Initializer.Declaration.Declarators[0];
        var initializer = (IExpressionInitializer) variable.Initial;

        var lengthAccess = factory.CreateReferenceExpression("expr.$0", LengthPropertyName);
        lengthAccess = initializer.Value.ReplaceBy(lengthAccess);
        lengthAccess.QualifierExpression.NotNull().ReplaceBy(expression);
        lengthAccess.ReplaceBy(factory.CreateExpression("$0 - 1", lengthAccess));
      }
    }
  }
}