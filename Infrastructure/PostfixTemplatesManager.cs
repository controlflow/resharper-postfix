using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.Application;
using JetBrains.Application.Settings;
using JetBrains.ReSharper.Feature.Services.Lookup;
using JetBrains.ReSharper.PostfixTemplates.Settings;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.PostfixTemplates
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
          providerType.GetCustomAttributes(typeof(PostfixTemplateAttribute), inherit: false);

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
      [CanBeNull] ITreeNode position, [NotNull] PostfixExecutionContext context,
      [CanBeNull] string templateName = null)
    {
      var postfixContext = IsAvailable(position, context);
      if (postfixContext == null || postfixContext.Expressions.Count == 0 || position == null)
        return EmptyList<ILookupItem>.InstanceList;

      var store = position.GetSettingsStore();
      var settings = store.GetKey<PostfixTemplatesSettings>(SettingsOptimization.OptimizeDefault);
      settings.DisabledProviders.SnapshotAndFreeze();

      var referencedElement = postfixContext.InnerExpression.ReferencedElement;
      if (referencedElement is INamespace) return EmptyList<ILookupItem>.InstanceList;

      var isTypeExpression = referencedElement is ITypeElement;
      var items = new List<ILookupItem>();

      foreach (var info in myTemplateProvidersInfos)
      {
        // check disabled providers
        {
          bool isEnabled;
          if (!settings.DisabledProviders.TryGet(info.SettingsKey, out isEnabled))
            isEnabled = !info.Metadata.DisabledByDefault;

          if (!isEnabled) continue;
        }

        if (templateName != null)
        {
          var name = info.Metadata.TemplateName;
          if (!string.Equals(templateName, name, StringComparison.Ordinal)) continue;
        }

        if (isTypeExpression && !info.Metadata.WorksOnTypes) continue;

        var lookupItem = info.Provider.CreateItem(postfixContext);
        if (lookupItem != null) items.Add(lookupItem);
      }

      return items;
    }

    [CanBeNull] public PostfixTemplateContext IsAvailable(
      [CanBeNull] ITreeNode position, [NotNull] PostfixExecutionContext executionContext)
    {
      if (!(position is ICSharpIdentifier)) return null;

      // expr.__
      var referenceExpression = position.Parent as IReferenceExpression;
      if (referenceExpression != null && referenceExpression.Delimiter != null)
      {
        var expression = referenceExpression.QualifierExpression;
        if (expression != null)
        {
          return new ReferenceExpressionPostfixTemplateContext(referenceExpression, expression, executionContext);
        }
      }

      // String.__
      var referenceName = position.Parent as IReferenceName;
      if (referenceName != null && referenceName.Qualifier != null && referenceName.Delimiter != null)
      {
        var typeUsage = referenceName.Parent as ITypeUsage;
        if (typeUsage != null)
        {
          var expression = typeUsage.Parent as ICSharpExpression;
          if (expression != null)
          {
            return new ReferenceNamePostfixTemplateContext(referenceName, expression, executionContext);
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
          var expression = FindExpressionBrokenByKeyword(expressionStatement);
          if (expression != null)
          {
            return new BrokenStatementPostfixTemplateContext(brokenStatement, expression, executionContext);
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
    private static ICSharpExpression FindExpressionBrokenByKeyword([NotNull] IExpressionStatement statement)
    {
      ICSharpExpression expression = null;
      var errorFound = false;

      for (ITreeNode treeNode = statement, last;; treeNode = last)
      {
        last = treeNode.LastChild; // inspect all the last nodes traversing up tree
        if (last == null) break;

        if (!errorFound) errorFound = last is IErrorElement;
        if (errorFound && last.Parent is ITypeUsage)
        {
          expression = last.Parent.Parent as ICSharpExpression;
          break;
        }

        // skip "expression statement is not closed with ';'" and friends
        while (last is IErrorElement) last = last.PrevSibling;

        if (last == null) break;
      }

      return expression;
    }
  }
}