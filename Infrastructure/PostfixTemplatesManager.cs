using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.Application;
using JetBrains.Application.Settings;
using JetBrains.DocumentModel;
using JetBrains.ReSharper.ControlFlow.PostfixCompletion.Settings;
using JetBrains.ReSharper.Feature.Services.Lookup;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Parsing;
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
          providerType.GetCustomAttributes(typeof (PostfixTemplateAttribute), false);
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
      [NotNull] ITreeNode node, bool forceMode, [NotNull] PostfixExecutionContext context)
    {
      if (!(node is ICSharpIdentifier) && !(node is ITokenNode))
        return EmptyList<ILookupItem>.InstanceList;

      // 'booleanExpr.in' - unexpected identifier (keyword)
      if (node.Parent is IErrorElement
          && node.Parent.LastChild == node
          && node.Parent.FirstChild == node) node = node.Parent;

      // simple case: 'anyExpr.notKeyworkShortcut'
      var referenceExpression = node.Parent as IReferenceExpression;
      if (referenceExpression != null)
      {
        var expression = referenceExpression.QualifierExpression;
        if (expression != null)
        {
          return CollectAvailableTemplates(
            referenceExpression, expression,
            DocumentRange.InvalidRange, forceMode, context);
        }
      }

      // handle collisions with C# keyword names 'lines.foreach' =>
      // ExpressionStmt(ReferenceExpr(lines.;Error);Error);ForeachStmt(foreach;Error)
      var reparse = context.ReparsedContext;
      if (reparse == null)
      {
        var expressionStatement = LookForKeywordBrokenExpressionStatement(node);
        if (expressionStatement != null)
        {
          var expression = FindExpressionBrokenByKeyword(expressionStatement);
          var referenceExpr = ReferenceExpressionNavigator.GetByQualifierExpression(expression);
          if (expression != null && referenceExpr != null)
          {
            var expressionRange = expression.GetDocumentRange();
            var nodeRange = node.GetDocumentRange().TextRange;
            var replaceRange = expressionRange.SetEndTo(nodeRange.EndOffset);

            return CollectAvailableTemplates(
              referenceExpr, expression, replaceRange, forceMode, context);
          }
        }
      }

      // handle collisions with predefined types 'x as string.'
      if (node.Parent is IErrorElement)
      {
        var typeUsage = node.Parent.PrevSibling as ITypeUsage;
        if (typeUsage != null)
        {
          var expression = node.Parent.Parent as ICSharpExpression;
          if (expression != null)
          {
            var typeUsageRange = reparse.ToDocumentRange(typeUsage);
            var nodeRange = reparse.ToDocumentRange(node).TextRange;
            var replaceRange = typeUsageRange.SetEndTo(nodeRange.EndOffset);

            return CollectAvailableTemplates(
              typeUsage, expression, replaceRange, forceMode, context);
          }
        }
      }

      // handle collisions with user types 'x as String.'
      var referenceName = node.Parent as IReferenceName;
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
            var qualifierRange = reparse.ToDocumentRange(referenceName.Qualifier);
            var delimiterRange = reparse.ToDocumentRange(referenceName.Delimiter);
            var replaceRange = qualifierRange.SetEndTo(delimiterRange.TextRange.EndOffset);

            return CollectAvailableTemplates(
              referenceName, expression, replaceRange, forceMode, context);
          }
        }
      }

      var brokenStatement = FindBrokenStatement(node);
      if (brokenStatement != null)
      {
        var expressionStatement = brokenStatement.PrevSibling as IExpressionStatement;
        if (expressionStatement != null)
        {
          ITreeNode coolNode;
          var expression = FindExpressionBrokenByKeyword2(expressionStatement, out coolNode);
          if (expression != null)
          {
            var expressionRange = reparse.ToDocumentRange(expression);
            var nodeRange = reparse.ToDocumentRange(node);
            var replaceRange = expressionRange.SetEndTo(nodeRange.TextRange.EndOffset);

            return CollectAvailableTemplates(
              coolNode /* TODO: ? */, expression, replaceRange, forceMode, context);
          }
        }
      }

      return EmptyList<ILookupItem>.InstanceList;
    }

    [CanBeNull]
    private static IExpressionStatement LookForKeywordBrokenExpressionStatement([NotNull] ITreeNode node)
    {
      if (node is IErrorElement)
      {
        // handle 'a > 0.in'
        var expression = node.Parent as ICSharpExpression;
        if (expression != null && expression.LastChild == node)
        {
          return expression.GetContainingNode<IExpressionStatement>();
        }

        // handle 'expr.else'
        if (node.Parent is IBlock)
        {
          var tokenNode = node.FirstChild as ITokenNode;
          if (tokenNode != null && tokenNode.GetTokenType().IsKeyword)
          {
            return node.PrevSibling as IExpressionStatement;
          }
        }
      }
      else
      {
        // handle 'lines.foreach'
        var statement = node.Parent as ICSharpStatement;
        if (statement != null && statement.FirstChild == node)
        {
          return statement.GetPreviousStatementInBlock() as IExpressionStatement;
        }

        // handle 'int.new' or 'int.typeof'
        var expression = node.Parent as ICSharpExpression;
        if (expression is IObjectCreationExpression || expression is ITypeofExpression)
        {
          var exprStatement = ExpressionStatementNavigator.GetByExpression(expression);
          if (exprStatement != null && exprStatement.FirstChild == expression)
          {
            return exprStatement.GetPreviousStatementInBlock() as IExpressionStatement;
          }
        }
      }

      return null;
    }

    [CanBeNull]
    private static ICSharpExpression FindExpressionBrokenByKeyword([NotNull] IExpressionStatement statement)
    {
      ICSharpExpression expression = null;
      var errorFound = false;

      for (ITreeNode treeNode = statement, last; expression == null; treeNode = last)
      {
        last = treeNode.LastChild; // inspect all the last nodes traversing up tree
        if (last == null) break;

        if (!errorFound) errorFound = last is IErrorElement;

        if (errorFound && last.Parent is IReferenceExpression)
        {
          var dot = (last is IErrorElement ? last.PrevSibling : last) as ITokenNode;
          if (dot != null && dot.GetTokenType() == CSharpTokenType.DOT)
          {
            ITreeNode node = dot;

            // skip whitespace in case of "expr   .if"
            var wsToken = dot.PrevSibling as ITokenNode;
            if (wsToken != null && wsToken.GetTokenType() == CSharpTokenType.WHITE_SPACE)
              node = wsToken;

            expression = node.PrevSibling as ICSharpExpression; //todo: comments?
          }
        }

        // skip "expression statement is not closed with ';'" and friends
        while (last is IErrorElement) last = last.PrevSibling;
        if (last == null) break;
      }

      return expression;
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

    [NotNull]
    private IList<ILookupItem> CollectAvailableTemplates(
      [NotNull] ITreeNode reference, [NotNull] ICSharpExpression expression,
      DocumentRange replaceRange, bool forceMode, [NotNull] PostfixExecutionContext context)
    {
      var postfixContext = new PostfixTemplateContext(
        reference, expression, replaceRange, forceMode, context);

      if (postfixContext.Expressions.Count == 0)
        return EmptyList<ILookupItem>.InstanceList;

      var store = expression.GetSettingsStore();
      var settings = store.GetKey<PostfixCompletionSettings>(SettingsOptimization.OptimizeDefault);
      settings.DisabledProviders.SnapshotAndFreeze();

      var isTypeExpression = postfixContext.InnerExpression.ReferencedElement is ITypeElement;
      var items = new List<ILookupItem>();

      var templateName = context.SpecificTemplateName;
      foreach (var info in myTemplateProvidersInfos)
      {
        bool isEnabled;
        if (!settings.DisabledProviders.TryGet(info.SettingsKey, out isEnabled))
          isEnabled = !info.Metadata.DisabledByDefault;

        if (!isEnabled) continue; // check disabled providers

        if (templateName != null)
        {
          var name = info.Metadata.TemplateName;
          if (!string.Equals(templateName, name, StringComparison.Ordinal))
            continue;
        }

        if (isTypeExpression && !info.Metadata.WorksOnTypes) continue;

        var lookupItem = info.Provider.CreateItems(postfixContext);
        if (lookupItem != null) items.Add(lookupItem);
      }

      if (templateName != null) // do not like it
        items.RemoveAll(x => x.Identity != templateName);

      return items;
    }
  }
}