using JetBrains.Annotations;
using JetBrains.ProjectModel;
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
using JetBrains.ReSharper.Psi.Util;
using JetBrains.ReSharper.Psi.Xaml.Impl;
using JetBrains.TextControl;
using JetBrains.Util;

// todo: do not create var if expr type is IDisposable itself?

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
      if (!expressionContext.CanBeStatement) return null;

      var expression = expressionContext.Expression;

      if (context.IsAutoCompletion)
      {
        if (!expressionContext.Type.IsResolved) return null;

        var predefinedType = expression.GetPredefinedType();
        var conversionRule = expression.GetTypeConversionRule();
        if (!expressionContext.ExpressionType
          .IsImplicitlyConvertibleTo(predefinedType.IDisposable, conversionRule))
          return null;
      }

      // check expression is local variable reference
      var resourceVariable = expressionContext.ReferencedElement as ILocalVariable;

      ITreeNode node = expression;
      while (true)
      {
        // inspect containing using statements
        var usingStatement = node.GetContainingNode<IUsingStatement>();
        if (usingStatement == null) break;

        // check if expressions is variable declared with using statement
        var declaration = usingStatement.Declaration;
        if (resourceVariable != null && declaration != null)
        {
          foreach (var member in declaration.DeclaratorsEnumerable)
          {
            if (Equals(member.DeclaredElement, resourceVariable))
              return null;
          }
        }

        // check expression is already in using statement expression
        if (declaration == null)
        {
          foreach (var e in usingStatement.ExpressionsEnumerable)
          {
            if (MiscUtil.AreExpressionsEquivalent(e, expression))
              return null;
          }
        }

        node = usingStatement;
      }

      return new UsingItem(expressionContext);
    }

    private sealed class UsingItem : StatementPostfixLookupItem<IUsingStatement>
    {
      [NotNull] private readonly LiveTemplatesManager myTemplatesManager;

      public UsingItem([NotNull] PrefixExpressionContext context) : base("using", context)
      {
        myTemplatesManager = context.PostfixContext.ExecutionContext.LiveTemplatesManager;
      }

      protected override IUsingStatement CreateStatement(CSharpElementFactory factory,
                                                         ICSharpExpression expression)
      {
        var template = "using (T x = $0)" + EmbeddedStatementBracesTemplate;
        return (IUsingStatement) factory.CreateStatement(template, expression);
      }

      protected override void AfterComplete(ITextControl textControl, IUsingStatement statement)
      {
        base.AfterComplete(textControl, statement);

        var declaration = (ILocalVariableDeclaration) statement.Declaration.Declarators[0];
        var typeExpression = new MacroCallExpressionNew(new SuggestVariableTypeMacroDef());
        var nameExpression = new MacroCallExpressionNew(new SuggestVariableNameMacroDef());

        var typeSpot = new HotspotInfo(
          new TemplateField("type", typeExpression, 0),
          declaration.TypeUsage.GetDocumentRange());

        var nameSpot = new HotspotInfo(
          new TemplateField("name", nameExpression, 0),
          declaration.NameIdentifier.GetDocumentRange());

        var session = myTemplatesManager.CreateHotspotSessionAtopExistingText(
          statement.GetSolution(), new TextRange(textControl.Caret.Offset()), textControl,
          LiveTemplatesManager.EscapeAction.LeaveTextAndCaret, typeSpot, nameSpot);

        session.Execute();
      }
    }
  }
}