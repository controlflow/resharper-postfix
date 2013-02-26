using JetBrains.ReSharper.Feature.Services.CSharp.CodeCompletion.Infrastructure;
using JetBrains.ReSharper.Feature.Services.CodeCompletion;
using JetBrains.ReSharper.Feature.Services.Lookup;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ProjectModel;

namespace JetBrains.ReSharper.ControlFlow.PostfixCompletion
{
  [Language(typeof(CSharpLanguage))]
  public class CSharpSurroundItemProviderSmart : CSharpItemsProviderBase<CSharpCodeCompletionContext>
  {
    protected override bool IsAvailable(CSharpCodeCompletionContext context)
    {
      var completionType = context.BasicContext.CodeCompletionType;
      return completionType == CodeCompletionType.AutomaticCompletion
          || completionType == CodeCompletionType.BasicCompletion;
    }

    protected override bool AddLookupItems(CSharpCodeCompletionContext context, GroupedItemsCollector collector)
    {
      var node = context.NodeInFile;
      if (node == null) return false;
      if (!(node is ICSharpIdentifier) && !(node is ITokenNode)) return false;

      var referenceExpression = node.Parent as IReferenceExpression;
      if (referenceExpression == null) return false;

      var expression = referenceExpression.QualifierExpression;
      if (expression == null) return false;

      var canBeStatement = (ExpressionStatementNavigator.GetByExpression(referenceExpression) != null);
      var qualifierType = expression.Type();

      var exprRange = expression.GetDocumentRange().TextRange;
      var replaceRange = referenceExpression.GetDocumentRange().TextRange;

      var solution = referenceExpression.GetSolution();
      foreach (var provider in solution.GetComponents<IPostfixTemplateProvider>())
      {
        foreach (var lookupItem in provider.CreateItems(
          referenceExpression, expression, qualifierType, canBeStatement))
        {
          lookupItem.InitializeRanges(exprRange, replaceRange);
          collector.AddAtDefaultPlace(lookupItem);
        }
      }

      return true;
    }
  }
}