using System.Collections.Generic;
using JetBrains.ReSharper.ControlFlow.PostfixCompletion.LookupItems;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;

namespace JetBrains.ReSharper.ControlFlow.PostfixCompletion.TemplateProviders
{
  [PostfixTemplateProvider("while", "Iterate while boolean statement is true")]
  public class WhileLoopTemplateProvider : IPostfixTemplateProvider
  {
    public IEnumerable<PostfixLookupItem> CreateItems(
      IReferenceExpression referenceExpression, ICSharpExpression expression, IType expressionType, bool canBeStatement)
    {
      if (canBeStatement && expressionType.IsBool())
        yield return new PostfixLookupItem("while", "while ($EXPR$) $CARET$");
    }
  }
}