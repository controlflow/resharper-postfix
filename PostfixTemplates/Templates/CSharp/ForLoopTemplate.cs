using JetBrains.Annotations;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.LiveTemplates;
using JetBrains.ReSharper.PostfixTemplates.CodeCompletion;
using JetBrains.ReSharper.PostfixTemplates.Contexts.CSharp;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.PostfixTemplates.Templates.CSharp
{
  // todo: apply var code style in R# 9.0

  [PostfixTemplate(
    templateName: "for",
    description: "Iterates over collection with index",
    example: "for (var i = 0; i < xs.Length; i++)")]
  public class ForLoopTemplate : ForLoopTemplateBase
  {
    [NotNull] private readonly LiveTemplatesManager myLiveTemplatesManager;

    public ForLoopTemplate([NotNull] LiveTemplatesManager liveTemplatesManager)
    {
      myLiveTemplatesManager = liveTemplatesManager;
    }

    public override PostfixTemplateInfo TryCreateInfo(CSharpPostfixTemplateContext context)
    {
      string lengthName;
      if (!CanBeLoopedOver(context, out lengthName)) return null;

      var expressionContext = context.InnerExpression;
      if (expressionContext == null) return null;

      return new ForLoopPostfixTemplateInfo("for", expressionContext, lengthName);
    }

    protected override PostfixTemplateBehavior CreateBehavior(ForLoopPostfixTemplateInfo info)
    {
      return new CSharpForLoopStatementBehavior(info, myLiveTemplatesManager);
    }

    private sealed class CSharpForLoopStatementBehavior : CSharpForLoopStatementBehaviorBase
    {
      public CSharpForLoopStatementBehavior(
        [NotNull] ForLoopPostfixTemplateInfo info, [NotNull] LiveTemplatesManager liveTemplatesManager)
        : base(info, liveTemplatesManager) { }

      protected override IForStatement CreateStatement(CSharpElementFactory factory, ICSharpExpression expression)
      {
        var template = "for(var x=0;x<$0;x++)" + EmbeddedStatementBracesTemplate;
        var forStatement = (IForStatement) factory.CreateStatement(template, expression);

        var condition = (IRelationalExpression) forStatement.Condition;
        if (LengthName == null)
        {
          condition.RightOperand.ReplaceBy(expression);
        }
        else
        {
          var lengthAccess = factory.CreateReferenceExpression("expr.$0", LengthName);
          lengthAccess = condition.RightOperand.ReplaceBy(lengthAccess);
          lengthAccess.QualifierExpression.NotNull().ReplaceBy(expression);
        }

        return forStatement;
      }
    }
  }
}