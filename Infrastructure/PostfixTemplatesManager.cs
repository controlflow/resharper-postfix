using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.Application;
using JetBrains.Application.Settings;
using JetBrains.ReSharper.ControlFlow.PostfixCompletion.Settings;
using JetBrains.ReSharper.Feature.Services.Lookup;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.ControlFlow.PostfixCompletion
{
  [ShellComponent]
  public class PostfixTemplatesManager
  {
    [NotNull] private readonly IList<TemplateProviderInfo> myTemplateProvidersInfos;

    public PostfixTemplatesManager([NotNull] IEnumerable<IPostfixTemplate> providers)
    {
      var infos = new List<TemplateProviderInfo>();
      foreach (var provider in providers)
      {
        var providerType = provider.GetType();
        var attributes = (PostfixTemplateAttribute[])
          providerType.GetCustomAttributes(
            typeof(PostfixTemplateAttribute), inherit: false);

        if (attributes.Length == 1)
        {
          var info = new TemplateProviderInfo(provider, providerType.FullName, attributes[0]);
          infos.Add(info);
        }
      }

      myTemplateProvidersInfos = infos.AsReadOnly();
    }

    public sealed class TemplateProviderInfo
    {
      [NotNull] public IPostfixTemplate Provider { get; private set; }
      [NotNull] public PostfixTemplateAttribute Metadata { get; private set; }
      [NotNull] public string SettingsKey { get; private set; }

      public TemplateProviderInfo([NotNull] IPostfixTemplate provider,
        [NotNull] string providerKey, [NotNull] PostfixTemplateAttribute metadata)
      {
        Provider = provider;
        Metadata = metadata;
        SettingsKey = providerKey;
      }
    }

    [NotNull] public IList<TemplateProviderInfo> TemplateProvidersInfos
    {
      get { return myTemplateProvidersInfos; }
    }

    [NotNull] public IList<ILookupItem> GetAvailableItems(
      [NotNull] ITreeNode node, [NotNull] PostfixExecutionContext context)
    {
      var postfixContext = IsAvailable(node, context);
      if (postfixContext == null || postfixContext.Expressions.Count == 0)
      {
        return EmptyList<ILookupItem>.InstanceList;
      }

      var store = node.GetSettingsStore();
      var settings = store.GetKey<PostfixCompletionSettings>(SettingsOptimization.OptimizeDefault);
      settings.DisabledProviders.SnapshotAndFreeze();

      var isTypeExpression = postfixContext.InnerExpression.ReferencedElement is ITypeElement;
      var items = new List<ILookupItem>();

      //var templateName = context.SpecificTemplateName;
      foreach (var info in myTemplateProvidersInfos)
      {
        bool isEnabled;
        if (!settings.DisabledProviders.TryGet(info.SettingsKey, out isEnabled))
          isEnabled = !info.Metadata.DisabledByDefault;

        if (!isEnabled) continue; // check disabled providers

        //if (templateName != null)
        //{
        //  var name = info.Metadata.TemplateName;
        //  if (!string.Equals(templateName, name, StringComparison.Ordinal))
        //    continue;
        //}

        if (isTypeExpression && !info.Metadata.WorksOnTypes) continue;

        var lookupItem = info.Provider.CreateItems(postfixContext);
        if (lookupItem != null) items.Add(lookupItem);
      }

      //if (templateName != null) // do not like it
      //{
      //  for (var index = items.Count - 1; index >= 0; index--)
      //  {
      //    if (items[index].Identity == templateName)
      //      items.RemoveAt(index);
      //  }
      //}

      return items;
    }

    [CanBeNull] public PostfixTemplateContext IsAvailable(
      [NotNull] ITreeNode position, [NotNull] PostfixExecutionContext executionContext)
    {
      // expr.__
      if (position is ICSharpIdentifier)
      {
        var referenceExpression = position.Parent as IReferenceExpression;
        if (referenceExpression != null)
        {
          var expression = referenceExpression.QualifierExpression;
          if (expression != null)
          {
            return new PostfixTemplateContext(
              referenceExpression, expression, executionContext);
          }
        }
      }

      // String.__
      var referenceName = position.Parent as IReferenceName;
      if (referenceName != null &&
          referenceName.Qualifier != null &&
          referenceName.Delimiter != null)
      {
        var usage = referenceName.Parent as ITypeUsage;
        if (usage != null)
        {
          var expression = usage.Parent as ICSharpExpression;
          if (expression != null)
          {
            return new PostfixTemplateContext(
              referenceName, expression, executionContext);
          }
        }
      }

      // string.__
      var brokenStatement = FindBrokenStatement(position);
      if (brokenStatement != null)
      {
        var expressionStatement = brokenStatement.PrevSibling as IExpressionStatement;
        if (expressionStatement != null)
        {
          ITreeNode coolNode;
          var expression = FindExpressionBrokenByKeyword2(expressionStatement, out coolNode);
          if (expression != null)
          {
            return new PostfixTemplateContext(
              coolNode, expression, executionContext);
          }
        }
      }

      

      return null;
    }

    [CanBeNull]
    private static ICSharpStatement FindBrokenStatement([NotNull] ITreeNode node)
    {
      var parent = node.Parent as ICSharpStatement;
      if (parent != null && parent.FirstChild == node && node.NextSibling is IErrorElement)
      {
        return parent;
      }

      var refExpr = node.Parent as IReferenceExpression;
      if (refExpr != null && refExpr.FirstChild == node)
      {
        if (refExpr.NextSibling is IErrorElement || !refExpr.IsPhysical())
        {
          return refExpr.Parent as IExpressionStatement;
        }
      }

      return null;
    }

    [CanBeNull]
    private static ICSharpExpression FindExpressionBrokenByKeyword2(
      [NotNull] IExpressionStatement statement, out ITreeNode coolNode)
    {
      ICSharpExpression expression = null;
      var errorFound = false;
      coolNode = null;

      for (ITreeNode treeNode = statement, last;; treeNode = last)
      {
        last = treeNode.LastChild; // inspect all the last nodes traversing up tree
        if (last == null) break;

        if (!errorFound) errorFound = last is IErrorElement;
        if (errorFound && last.Parent is ITypeUsage)
        {
          if (last is IReferenceName && last.Parent is IUserTypeUsage)
            coolNode = last;

          expression = last.Parent.Parent as ICSharpExpression;
          break;
        }

        // skip "expression statement is not closed with ';'" and friends
        while (last is IErrorElement)
        {
          coolNode = last;
          last = last.PrevSibling;
        }

        if (last == null) break;
      }

      return expression;
    }
  }
}