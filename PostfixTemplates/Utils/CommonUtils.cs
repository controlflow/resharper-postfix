using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.DocumentModel;
using JetBrains.ReSharper.Feature.Services.CodeCompletion;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.AspectLookupItems.BaseInfrastructure;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.AspectLookupItems.Info;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.LookupItems;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Web.CodeBehindSupport;
using JetBrains.Util;

namespace JetBrains.ReSharper.PostfixTemplates
{
  [PublicAPI]
  public static class CommonUtils
  {
    [CanBeNull]
    public static TReturn DoTransaction<TReturn>(
      [NotNull] this IPsiServices services, [NotNull] string commandName, [NotNull, InstantHandle] Func<TReturn> func)
    {
      var value = default(TReturn);
      services.Transactions.Execute(commandName, () =>
      {
        value = func();
      });

      return value;
    }

    public static DocumentRange ToDocumentRange([CanBeNull] this ReparsedCodeCompletionContext reparsedContext, [NotNull] ITreeNode treeNode)
    {
      var documentRange = treeNode.GetDocumentRange();

      if (reparsedContext == null) return documentRange;

      var reparsedTreeRange = treeNode.GetTreeTextRange();

      var document = documentRange.Document;
      if (document != null)
      {
        var originalDocRange = reparsedContext.ToDocumentRange(reparsedTreeRange);
        return new DocumentRange(document, originalDocRange);
      }
      else
      {


        // todo: maybe rewrite this without range translator knowledge

        var originalGeneratedTreeRange = reparsedContext.ToOriginalTreeRange(reparsedTreeRange);
        var sandBox = treeNode.GetContainingNode<ISandBox>().NotNull("sandBox != null");

        var contextNode = sandBox.ContextNode.NotNull("sandBox.ContextNode != null");
        var containingFile = contextNode.GetContainingFile().NotNull("containingFile != null");

        // todo: check for IFileImpl

        var translator = containingFile.GetRangeTranslator();
        var originalTreeRange = translator.GeneratedToOriginal(originalGeneratedTreeRange);
        var originalDocRange = translator.OriginalFile.GetDocumentRange(originalTreeRange);

        return originalDocRange;
      }
    }

    [NotNull]
    public static string GetParenthesesTemplate(this ParenthesesInsertType parenthesesType, bool atStatementEnd = false)
    {
      switch (parenthesesType)
      {
        case ParenthesesInsertType.None: return "";
        case ParenthesesInsertType.Left: return "(";
      }

      return atStatementEnd ? "();" : "()";
    }

    // todo: remove
    [NotNull]
    public static IEnumerable<DeclaredElementInstance> GetAllDeclaredElementInstances([NotNull] this ILookupItem lookupItem)
    {
      var wrapper = lookupItem as IAspectLookupItem<DeclaredElementInfo>;
      if (wrapper != null) return wrapper.Info.AllDeclaredElements;

      return EmptyList<DeclaredElementInstance>.InstanceList;
    }

    // todo: remove
    [CanBeNull]
    public static DeclaredElementInstance GetDeclaredElement([NotNull] this ILookupItem lookupItem)
    {
      var wrapper = lookupItem as IAspectLookupItem<DeclaredElementInfo>;
      if (wrapper == null) return null;

      return wrapper.Info.PreferredDeclaredElement;
    }
  }
}