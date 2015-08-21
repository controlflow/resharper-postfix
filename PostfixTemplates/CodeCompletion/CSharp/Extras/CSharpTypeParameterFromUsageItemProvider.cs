using System.Drawing;
using System.Globalization;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.CodeCompletion;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.AspectLookupItems.BaseInfrastructure;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.AspectLookupItems.Behaviors;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.AspectLookupItems.Info;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.AspectLookupItems.Matchers;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.LookupItems;
using JetBrains.ReSharper.Feature.Services.CSharp.CodeCompletion.Infrastructure;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.Hotspots;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.LiveTemplates;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.Templates;
using JetBrains.ReSharper.Feature.Services.Lookup;
using JetBrains.ReSharper.Feature.Services.Util;
using JetBrains.ReSharper.Features.Intellisense.CodeCompletion.CSharp.Rules;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Resources.Shell;
using JetBrains.Text;
using JetBrains.TextControl;
using JetBrains.UI.RichText;
using JetBrains.Util;

namespace JetBrains.ReSharper.PostfixTemplates.CodeCompletion.CSharp
{
  [Language(typeof(CSharpLanguage))]
  public class CSharpTypeParameterFromUsageItemProvider : CSharpItemsProviderBase<CSharpCodeCompletionContext>
  {
    protected override bool IsAvailable(CSharpCodeCompletionContext context)
    {
      return context.BasicContext.CodeCompletionType == CodeCompletionType.BasicCompletion;
    }

    protected override bool AddLookupItems(CSharpCodeCompletionContext context, GroupedItemsCollector collector)
    {
      var unterminatedContext = context.UnterminatedContext;
      if (unterminatedContext == null) return false;

      var referenceNameReference = unterminatedContext.Reference as IReferenceNameReference;
      if (referenceNameReference == null) return false;

      var referenceName = referenceNameReference.GetTreeNode() as IReferenceName;
      if (referenceName == null) return false;

      var methodDeclaration = referenceName.GetContainingTypeMemberDeclaration() as IMethodDeclaration;
      if (methodDeclaration == null) return false; // only methods can be generic

      if (!IsInsideSignatureWhereTypeUsageExpected(methodDeclaration, referenceName)) return false;

      var typeParameterName = GetTypeParameterName(methodDeclaration);
      var postfixInfo = new TextualInfo(typeParameterName, typeParameterName, context.BasicContext);

      var lookupItem = LookupItemFactory.CreateLookupItem(postfixInfo)
        .WithPresentation(item =>
        {
          var presentation = new PostfixTemplatePresentation(typeParameterName);
          var grayText = TextStyle.FromForeColor(Color.Gray);
          presentation.DisplayName.Append("*", grayText);
          presentation.DisplayTypeName = new RichText("(create type parameter)", grayText);
          return presentation;
        })
        .WithMatcher(item => new TextualMatcher<ILookupItemInfo>(item.Info, IdentifierMatchingStyle.Default))
        .WithBehavior(item => new TypeParameterFromUsageBehavior(item.Info));

      collector.Add(lookupItem);
      return true;
    }

    [NotNull]
    private static string GetTypeParameterName([NotNull] IMethodDeclaration methodDeclaration)
    {
      var typeParameterList = methodDeclaration.TypeParameterList;
      if (typeParameterList == null) return "T";

      var usedNames = typeParameterList.TypeParameterDeclarations.Select(x => x.DeclaredName).ToHashSet();

      var name = "T";
      for (var index = 2; index < 100; index++)
      {
        if (!usedNames.Contains(name)) break;

        name = "T" + index.ToString(CultureInfo.InvariantCulture);
      }

      return name;
    }

    private sealed class TypeParameterFromUsageBehavior : TextualBehavior<TextualInfo>
    {
      public TypeParameterFromUsageBehavior([NotNull] TextualInfo info) : base(info) { }

      public override void Accept(
        ITextControl textControl, TextRange nameRange, LookupItemInsertType lookupItemInsertType,
        Suffix suffix, ISolution solution, bool keepCaretStill)
      {
        base.Accept(textControl, nameRange, lookupItemInsertType, suffix, solution, keepCaretStill);

        var psiServices = solution.GetPsiServices();
        var startOffset = nameRange.StartOffset;
        var suffix1 = suffix;

        psiServices.Files.CommitAllDocumentsAsync(() =>
        {
          var hotspotInfo = psiServices.DoTransaction(typeof(CSharpTypeParameterFromUsageItemProvider).FullName, () =>
          {
            using (WriteLockCookie.Create(true))
            {
              var typeParameterName = Info.Text;
              var referenceName = TextControlToPsi
                .GetElements<IReferenceName>(solution, textControl.Document, startOffset)
                .FirstOrDefault(x => x.NameIdentifier != null && x.NameIdentifier.Name == typeParameterName);
              if (referenceName == null) return null;

              var methodDeclaration = referenceName.GetContainingNode<IMethodDeclaration>();
              if (methodDeclaration == null) return null;

              var factory = CSharpElementFactory.GetInstance(methodDeclaration);

              var lastTypeParameter = methodDeclaration.TypeParameterDeclarations.LastOrDefault();
              var newTypeParameter = methodDeclaration.AddTypeParameterAfter(
                factory.CreateTypeParameterOfMethodDeclaration(typeParameterName), anchor: lastTypeParameter);

              //var endOffset = referenceName.GetDocumentRange().TextRange.EndOffset;
              //textControl.Caret.MoveTo(endOffset, CaretVisualPlacement.DontScrollIfVisible);

              return new HotspotInfo(new TemplateField(typeParameterName, initialRange: 1),
                documentRanges: new[] { newTypeParameter.GetDocumentRange(), referenceName.GetDocumentRange() });
            }
          });

          if (hotspotInfo == null) return;

          // do not provide hotspots when item is completed with spacebar
          if (suffix1.HasPresentation && suffix1.Presentation == ' ') return;

          //var endRange = hotspotInfo.Ranges[1].EndOffsetRange().TextRange;
          var endRange = TextRange.InvalidRange;

          var session = LiveTemplatesManager.Instance.CreateHotspotSessionAtopExistingText(
            solution, endRange, textControl, LiveTemplatesManager.EscapeAction.RestoreToOriginalText, hotspotInfo);

          session.Execute();
        });
      }
    }

    private static bool IsInsideSignatureWhereTypeUsageExpected([NotNull] IMethodDeclaration methodDeclaration, [NotNull] IReferenceName referenceName)
    {
      // require method declaration to at least have a name identifier
      if (methodDeclaration.NameIdentifier == null) return false;

      var returnTypeUsage = methodDeclaration.TypeUsage;
      if (returnTypeUsage != null) // THere M()
      {
        if (returnTypeUsage.Contains(referenceName)) return true;
      }

      if (methodDeclaration.LPar != null) // void M(int id, THere t, string s)
      {
        foreach (var parameterDeclaration in methodDeclaration.ParameterDeclarationsEnumerable)
        {
          if (parameterDeclaration.Contains(referenceName)) return true;
        }
      }

      if (methodDeclaration.RPar != null) // void M<T>() where T : THere
      {
        foreach (var constraintsClause in methodDeclaration.TypeParameterConstraintsClausesEnumerable)
        {
          if (constraintsClause.Contains(referenceName)) return true;
        }
      }

      return false;
    }
  }
}