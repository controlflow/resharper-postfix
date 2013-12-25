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

      var typeIsEnumerable = !context.IsAutoCompletion;
      if (!typeIsEnumerable)
      {
        if (!expressionContext.Type.IsResolved) return null;

        var predefined = expressionContext.Expression.GetPredefinedType();
        var rule = expressionContext.Expression.GetTypeConversionRule();
        if (rule.IsImplicitlyConvertibleTo(expressionContext.Type, predefined.IEnumerable))
        {
          typeIsEnumerable = true;
        }
      }

      if (!typeIsEnumerable)
      {
        var declaredType = expressionContext.Type as IDeclaredType;
        if (declaredType != null && !declaredType.IsUnknown)
        {
          var typeElement = declaredType.GetTypeElement();
          if (typeElement != null && typeElement.IsForeachEnumeratorPatternType())
          {
            typeIsEnumerable = true;
          }
        }
      }

      if (!typeIsEnumerable) return null;
      return new ForEachItem(expressionContext);
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
        base.AfterComplete(textControl, statement);

        var iterator = statement.IteratorDeclaration;
        var typeExpression = new MacroCallExpressionNew(new SuggestVariableTypeMacroDef());
        var nameExpression = new MacroCallExpressionNew(new SuggestVariableNameMacroDef());

        var typeSpot = new HotspotInfo(
          new TemplateField("type", typeExpression, 0),
          iterator.VarKeyword.GetDocumentRange().GetHotspotRange());

        var nameSpot = new HotspotInfo(
          new TemplateField("name", nameExpression, 0),
          iterator.NameIdentifier.GetDocumentRange().GetHotspotRange());

        var session = myTemplatesManager.CreateHotspotSessionAtopExistingText(
          statement.GetSolution(), new TextRange(textControl.Caret.Offset()), textControl,
          LiveTemplatesManager.EscapeAction.LeaveTextAndCaret, new[] {typeSpot, nameSpot});

        // special case: handle [.] suffix
        //if (suffix.HasPresentation && suffix.Presentation == '.')
        //{
        //  session.AdviceFinished((_, terminationType) =>
        //  {
        //    if (terminationType == TerminationType.Finished)
        //    {
        //      var nameValue = session.Hotspots[1].CurrentValue;
        //      textControl.Document.InsertText(textControl.Caret.Offset(), nameValue);
        //      suffix.Playback(textControl);
        //    }
        //  });
        //}

        session.Execute();
      }
    }
  }
}