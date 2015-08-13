using System.Drawing;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.DocumentModel;
using JetBrains.ReSharper.Feature.Services.CodeCompletion;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.AspectLookupItems.BaseInfrastructure;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.AspectLookupItems.Behaviors;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.AspectLookupItems.Matchers;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.AspectLookupItems.Presentations;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.LookupItems;
using JetBrains.ReSharper.Feature.Services.CSharp.CodeCompletion.Infrastructure;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.Hotspots;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.LiveTemplates;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.Templates;
using JetBrains.ReSharper.Feature.Services.Lookup;
using JetBrains.ReSharper.Feature.Services.Resources;
using JetBrains.ReSharper.Feature.Services.Util;
using JetBrains.ReSharper.Features.Intellisense.CodeCompletion.CSharp.Rules;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.ExpectedTypes;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Resources.Shell;
using JetBrains.Text;
using JetBrains.TextControl;
using JetBrains.UI.RichText;
using JetBrains.Util;

namespace JetBrains.ReSharper.PostfixTemplates.CodeCompletion
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

      var postfixInfo = new PostfixTemplateInfo("T", "T", context.BasicContext);
      postfixInfo.Ranges = context.CompletionRanges ?? GetDefaultRanges(context);

      var lookupItem = LookupItemFactory.CreateLookupItem(postfixInfo)
        .WithPresentation(item =>
        {
          var presentation = new TextualPresentation<ILookupItemInfo>(item.Info, ServicesThemedIcons.LiveTemplate.Id);
          presentation.DisplayTypeName = new RichText("(create type parameter)", TextStyle.FromForeColor(Color.Gray));
          return presentation;
        })
        .WithMatcher(item => new TextualMatcher<ILookupItemInfo>(item.Info, IdentifierMatchingStyle.Default))
        .WithBehavior(item =>
        {
          var behavior = new TypeParameterFromUsageBehavior(item.Info);
          behavior.InitializeRanges(context.CompletionRanges, context.BasicContext);
          return behavior;
        });

      collector.Add(lookupItem);
      return true;
    }

    private sealed class TypeParameterFromUsageBehavior : TextualBehavior<PostfixTemplateInfo>
    {
      public TypeParameterFromUsageBehavior([NotNull] PostfixTemplateInfo info) : base(info) { }

      protected override void OnAfterComplete(
        ITextControl textControl, ref TextRange nameRange, ref TextRange decorationRange,
        TailType tailType, ref Suffix suffix, ref IRangeMarker caretPositionRangeMarker)
      {
        base.OnAfterComplete(textControl, ref nameRange, ref decorationRange, tailType, ref suffix, ref caretPositionRangeMarker);

        var solution = Info.Context.Solution;
        var psiServices = solution.GetPsiServices();
        var startOffset = nameRange.StartOffset;

        var hotspotInfo = psiServices.DoTransaction(typeof(CSharpTypeParameterFromUsageItemProvider).FullName, () =>
        {
          using (WriteLockCookie.Create(true))
          {
            var referenceName = TextControlToPsi
              .GetElements<IReferenceName>(solution, textControl.Document, startOffset)
              .FirstOrDefault(x => x.NameIdentifier != null && x.NameIdentifier.Name == "T");
            if (referenceName == null) return null;

            var methodDeclaration = referenceName.GetContainingNode<IMethodDeclaration>();
            if (methodDeclaration == null) return null;

            var factory = CSharpElementFactory.GetInstance(methodDeclaration);

            var lastTypeParameter = methodDeclaration.TypeParameterDeclarations.LastOrDefault();
            var newTypeParameter = methodDeclaration.AddTypeParameterAfter(
              factory.CreateTypeParameterOfMethodDeclaration("T"), anchor: lastTypeParameter);

            return new HotspotInfo(new TemplateField("T", initialRange: 1),
              documentRanges: new[] { newTypeParameter.GetDocumentRange(), referenceName.GetDocumentRange() });
          }
        });

        if (hotspotInfo == null) return;

        // do not provide hotspots when item is completed with spacebar
        if (suffix.HasPresentation && suffix.Presentation == ' ') return;

        var session = LiveTemplatesManager.Instance.CreateHotspotSessionAtopExistingText(
          solution, TextRange.InvalidRange, textControl, LiveTemplatesManager.EscapeAction.RestoreToOriginalText, hotspotInfo);

        session.Execute();
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