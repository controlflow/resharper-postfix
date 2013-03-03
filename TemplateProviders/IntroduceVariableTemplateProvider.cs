using System.Collections.Generic;
using JetBrains.ReSharper.ControlFlow.PostfixCompletion.LookupItems;
using JetBrains.ReSharper.Feature.Services.Lookup;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;

namespace JetBrains.ReSharper.ControlFlow.PostfixCompletion.TemplateProviders
{
  [PostfixTemplateProvider("var", "Introduces variable for expression")]
  public class IntroduceVariableTemplateProvider : IPostfixTemplateProvider
  {
    public void CreateItems(PostfixTemplateAcceptanceContext context, ICollection<ILookupItem> consumer)
    {
      // todo: relax this restriction
      if (!context.CanBeStatement) return;

      // filter out too simple locals expressions
      var referenceExpression = context.Expression as IReferenceExpression;
      if (referenceExpression != null)
      {
        var declaredElement = referenceExpression.Reference.Resolve().DeclaredElement;
        if (declaredElement is IParameter || declaredElement is ILocalVariable)
          return;
      }

      consumer.Add(new NameSuggestionPostfixLookupItem(
        context, "var", "var $NAME$ = $EXPR$", context.Expression));
    }
  }
}