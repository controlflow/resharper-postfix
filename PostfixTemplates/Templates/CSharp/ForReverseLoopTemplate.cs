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
    templateName: "forr",
    description: "Iterates over collection in reverse with index",
    example: "for (var i = xs.Length-1; i >= 0; i--)")]
  public class ForReverseLoopTemplate : ForLoopTemplateBase
  {
    [NotNull] private readonly LiveTemplatesManager myLiveTemplatesManager;

    public ForReverseLoopTemplate([NotNull] LiveTemplatesManager liveTemplatesManager)
    {
      myLiveTemplatesManager = liveTemplatesManager;
    }

    public override PostfixTemplateInfo TryCreateInfo(CSharpPostfixTemplateContext context)
    {
      string lengthName;
      if (!CanBeLoopedOver(context, out lengthName)) return null;

      var expressionContext = context.InnerExpression;
      if (expressionContext == null) return null;

      return new ForLoopPostfixTemplateInfo("forr", expressionContext, lengthName);
    }

    protected override PostfixTemplateBehavior CreateBehavior(ForLoopPostfixTemplateInfo info)
    {
      return new CSharpRevereForLoopStatementBehavior(info, myLiveTemplatesManager);
    }

    private sealed class CSharpRevereForLoopStatementBehavior : CSharpForLoopStatementBehaviorBase
    {
      public CSharpRevereForLoopStatementBehavior(
        [NotNull] ForLoopPostfixTemplateInfo info, [NotNull] LiveTemplatesManager liveTemplatesManager)
        : base(info, liveTemplatesManager) { }

      protected override IForStatement CreateStatement(CSharpElementFactory factory, ICSharpExpression expression)
      {
        var hasLength = (LengthName != null);
        var template = hasLength ? "for(var x=$0;x>=0;x--)" : "for(var x=$0;x>0;x--)";
        var forStatement = (IForStatement) factory.CreateStatement(template + EmbeddedStatementBracesTemplate, expression);

        var variable = (ILocalVariableDeclaration) forStatement.Initializer.Declaration.Declarators[0];
        var initializer = (IExpressionInitializer) variable.Initial;

        if (!hasLength)
        {
          var value = initializer.Value.ReplaceBy(expression);
          value.ReplaceBy(value);
        }
        else
        {
          var lengthAccess = factory.CreateReferenceExpression("expr.$0", LengthName);
          lengthAccess = initializer.Value.ReplaceBy(lengthAccess);
          lengthAccess.QualifierExpression.NotNull().ReplaceBy(expression);
          lengthAccess.ReplaceBy(factory.CreateExpression("$0 - 1", lengthAccess));
        }

        return forStatement;
      }
    }
  }
}