using JetBrains.Application;
using JetBrains.Application.Settings;
using JetBrains.ReSharper.ControlFlow.PostfixCompletion.Settings;
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
  public class CSharpPostfixItemProviderSmart : CSharpItemsProviderBase<CSharpCodeCompletionContext>
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

      var settingsStore = expression.GetSettingsStore();
      var completionSettings = settingsStore.GetKey<PostfixCompletionSettings>(SettingsOptimization.OptimizeDefault);
      completionSettings.DisabledProviders.SnapshotAndFreeze();

      foreach (var provider in Shell.Instance.GetComponents<IPostfixTemplateProvider>())
      {
        var providerKey = provider.GetType().FullName;

        bool isEnabled;
        if (completionSettings.DisabledProviders.TryGet(providerKey, out isEnabled) && !isEnabled)
          continue; // check disabled providers

        foreach (var lookupItem in provider.CreateItems(
          referenceExpression, expression, qualifierType, canBeStatement))
        {
          lookupItem.InitializeRanges(exprRange, replaceRange);
          collector.AddAtDefaultPlace(lookupItem);
        }
      }

      return true;
    }

    // todo: transform?
  }
}