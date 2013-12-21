using JetBrains.Annotations;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.LiveTemplates;
using JetBrains.ReSharper.Feature.Services.Lookup;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.ControlFlow.PostfixCompletion.Templates
{
  [PostfixTemplate(
    templateName: "forr",
    description: "Iterates over collection in reverse with index",
    example: "for (var i = expr.Length - 1; i >= 0; i--)")]
  public class ForReverseLoopTemplate : ForLoopTemplateBase, IPostfixTemplate
  {
    [NotNull] private readonly LiveTemplatesManager myTemplatesManager;

    public ForReverseLoopTemplate([NotNull] LiveTemplatesManager templatesManager)
    {
      myTemplatesManager = templatesManager;
    }

    public ILookupItem CreateItem(PostfixTemplateContext context)
    {
      string lengthPropertyName;
      if (CreateItems(context, out lengthPropertyName))
      {
        return new ReverseForLookupItem(
          context.InnerExpression, myTemplatesManager, lengthPropertyName);
      }

      return null;
    }

    private sealed class ReverseForLookupItem : ForLookupItemBase
    {
      public ReverseForLookupItem(
        [NotNull] PrefixExpressionContext context,
        [NotNull] LiveTemplatesManager templatesManager,
        [CanBeNull] string lengthPropertyName)
        : base("forR", context, templatesManager, lengthPropertyName) { }

      protected override IForStatement CreateStatement(CSharpElementFactory factory, ICSharpExpression expression)
      {
        var template = "for(var x=$0;x>=0;x--)" + EmbeddedStatementBracesTemplate;
        var forStatement = (IForStatement) factory.CreateStatement(template, expression);

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

        return forStatement;
      }
    }
  }
}