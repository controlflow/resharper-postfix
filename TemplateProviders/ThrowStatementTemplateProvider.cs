using System.Collections.Generic;
using JetBrains.ReSharper.ControlFlow.PostfixCompletion.LookupItems;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Impl;
using JetBrains.ReSharper.Psi.CSharp.Tree;

namespace JetBrains.ReSharper.ControlFlow.PostfixCompletion.TemplateProviders
{
  [PostfixTemplateProvider("throw", "Throw expression of 'Exception' type")]
  public class ThrowStatementTemplateProvider : IPostfixTemplateProvider
  {
    public IEnumerable<PostfixLookupItem> CreateItems(
      ICSharpExpression expression, IType expressionType, bool canBeStatement)
    {
      if (canBeStatement && !expressionType.IsUnknown)
      {
        var conversionRule = expression.GetTypeConversionRule();
        var predefinedType = expression.GetPsiModule().GetPredefinedType();
        if (conversionRule.IsImplicitlyConvertibleTo(expressionType, predefinedType.Exception))
          yield return new PostfixLookupItem("throw", "throw $EXPR$");
      }
    }
  }
}