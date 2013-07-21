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
  [PostfixTemplateProvider("for", "Iterating over collections with length")]
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

        consumer.Add(new LookupItem(exprContext, lengthPropertyName));
      }
    }

    private sealed class LookupItem : KeywordStatementPostfixLookupItem<IForStatement>
    {
      [NotNull] private readonly string myLengthPropertyName;

      public LookupItem(
        [NotNull] PrefixExpressionContext context, [NotNull] string lengthPropertyName)
        : base("for", context)
      {
        myLengthPropertyName = lengthPropertyName;
      }

      protected override string Template { get { return "for(var x = 0; x < expr; x++)"; } }
      public override bool ShortcutIsCSharpStatementKeyword { get { return true; } }

      protected override void PlaceExpression(
        IForStatement forStatement, ICSharpExpression expression, CSharpElementFactory factory)
      {
        var condition = (IRelationalExpression) forStatement.Condition;
        var lengthAccess = factory.CreateReferenceExpression("expr.$0", myLengthPropertyName);
        lengthAccess = condition.RightOperand.ReplaceBy(lengthAccess);
        lengthAccess.QualifierExpression.NotNull().ReplaceBy(expression);
      }

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
  }
}