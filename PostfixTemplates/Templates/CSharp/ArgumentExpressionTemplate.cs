using JetBrains.Annotations;
using JetBrains.Application.Settings;
using JetBrains.DataFlow;
using JetBrains.DocumentModel;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.Hotspots;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.LiveTemplates;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.Templates;
using JetBrains.ReSharper.Feature.Services.Lookup;
using JetBrains.ReSharper.Feature.Services.Util;
using JetBrains.ReSharper.PostfixTemplates.Contexts.CSharp;
using JetBrains.ReSharper.PostfixTemplates.LookupItems;
using JetBrains.ReSharper.PostfixTemplates.Settings;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Parsing;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Resources.Shell;
using JetBrains.TextControl;
using JetBrains.Util;

namespace JetBrains.ReSharper.PostfixTemplates.Templates.CSharp
{
  [PostfixTemplate(
    templateName: "arg",
    description: "Surrounds expression with invocation",
    example: "Method(expr)")]
  public class ArgumentExpressionTemplate : IPostfixTemplate<CSharpPostfixTemplateContext>
  {
    [NotNull] private readonly LiveTemplatesManager myLiveTemplatesMananger;
    [NotNull] private readonly LookupItemsOwnerFactory myLookupItemsOwnerFactory;

    public ArgumentExpressionTemplate([NotNull] LiveTemplatesManager liveTemplatesMananger, [NotNull] LookupItemsOwnerFactory lookupItemsOwnerFactory)
    {
      myLiveTemplatesMananger = liveTemplatesMananger;
      myLookupItemsOwnerFactory = lookupItemsOwnerFactory;
    }

    public PostfixTemplateInfo TryCreateInfo(CSharpPostfixTemplateContext context)
    {
      if (context.IsPreciseMode) return null;

      // disable .arg template if .arg hotspot is enabled now
      var textControl = context.ExecutionContext.TextControl;
      if (textControl.GetData(PostfixArgTemplateExpansion) != null) return null;

      var expressions = CSharpPostfixUtis.FindExpressionWithValuesContexts(context, IsNiceArgument);
      if (expressions.Length == 0) return null;

      return new PostfixTemplateInfo("arg", expressions);
    }

    public PostfixTemplateBehavior CreateBehavior(PostfixTemplateInfo info)
    {
      return new CSharpPostfixArgumentExpressionBehavior(info, myLiveTemplatesMananger, myLookupItemsOwnerFactory);
    }

    private static bool IsNiceArgument([NotNull] ICSharpExpression expression)
    {
      if (expression is IAssignmentExpression) return false;

      return true;
    }

    [NotNull] private static readonly Key<object> PostfixArgTemplateExpansion = new Key(typeof(ArgumentExpressionTemplate).FullName);

    private class CSharpPostfixArgumentExpressionBehavior : CSharpExpressionPostfixTemplateBehavior<IInvocationExpression>
    {
      [NotNull] private readonly LiveTemplatesManager myTemplatesManager;
      [NotNull] private readonly LookupItemsOwnerFactory myLookupItemsOwnerFactory;

      public CSharpPostfixArgumentExpressionBehavior([NotNull] PostfixTemplateInfo info, [NotNull] LiveTemplatesManager templatesManager, LookupItemsOwnerFactory lookupItemsOwnerFactory) : base(info)
      {
        myTemplatesManager = templatesManager;
        myLookupItemsOwnerFactory = lookupItemsOwnerFactory;
      }

      protected override string ExpressionSelectTitle
      {
        get { return "Select argument expression"; }
      }

      protected override IInvocationExpression CreateExpression(CSharpElementFactory factory, ICSharpExpression expression)
      {
        return (IInvocationExpression) factory.CreateExpression("Method($0)", expression);
      }

      protected override void AfterComplete(ITextControl textControl, IInvocationExpression expression)
      {
        var invocationRange = expression.InvokedExpression.GetDocumentRange();
        var languageType = expression.Language;
        var hotspotInfo = new HotspotInfo(new TemplateField("Method", 0), invocationRange);

        var argumentRange = expression.Arguments[0].Value.GetDocumentRange();
        var solution = expression.GetSolution();

        var marker = argumentRange.EndOffsetRange().CreateRangeMarker();
        var length = (marker.Range.EndOffset - invocationRange.TextRange.EndOffset);

        var session = myTemplatesManager.CreateHotspotSessionAtopExistingText(
          expression.GetSolution(), TextRange.InvalidRange, textControl,
          LiveTemplatesManager.EscapeAction.RestoreToOriginalText, hotspotInfo);

        var settings = expression.GetSettingsStore();
        var invokeParameterInfo = settings.GetValue(PostfixSettingsAccessor.InvokeParameterInfo);

        textControl.PutData(PostfixArgTemplateExpansion, string.Empty);

        session.Closed.Advise(EternalLifetime.Instance, _ =>
        {
          textControl.PutData(PostfixArgTemplateExpansion, null);

          using (ReadLockCookie.Create())
          {
            var hotspotRange = session.Hotspots[0].RangeMarker.Range;
            if (!hotspotRange.IsValid) return;

            solution.GetPsiServices().Files.CommitAllDocuments();

            if (TryPlaceCaretSmart(solution, textControl, languageType, hotspotRange))
              return;

            var endOffset = hotspotRange.EndOffset + length;
            textControl.Caret.MoveTo(endOffset, CaretVisualPlacement.DontScrollIfVisible);

            if (invokeParameterInfo)
            {
              var lookupItemsOwner = myLookupItemsOwnerFactory.CreateLookupItemsOwner(textControl);
              LookupUtil.ShowParameterInfo(solution, textControl, lookupItemsOwner);
            }
          }
        });

        session.Execute();
      }

      private static bool TryPlaceCaretSmart([NotNull] ISolution solution, [NotNull] ITextControl textControl,
                                             [NotNull] PsiLanguageType language, TextRange range)
      {
        foreach (var tokenNode in TextControlToPsi.GetElements<ITokenNode>(solution, textControl.Document, range.EndOffset))
        {
          if (!tokenNode.Language.IsLanguage(language)) continue;

          var tokenType = tokenNode.GetTokenType();
          if (tokenType == CSharpTokenType.LT)
          {
            var typeArguments = tokenNode.Parent as ITypeArgumentList;
            if (typeArguments == null) continue;

            var endOffset = tokenNode.GetDocumentRange().TextRange.EndOffset;
            textControl.Caret.MoveTo(endOffset, CaretVisualPlacement.DontScrollIfVisible);
            return true;
          }

          if (tokenType == CSharpTokenType.LPARENTH)
          {
            var invocation = tokenNode.Parent as IInvocationExpression;
            if (invocation == null || invocation.RPar == null) continue;

            var reference = invocation.InvocationExpressionReference;
            if (reference.Resolve().ResolveErrorType == ResolveErrorType.OK)
            {
              var offset = invocation.RPar.GetDocumentRange().TextRange.EndOffset;
              textControl.Caret.MoveTo(offset, CaretVisualPlacement.DontScrollIfVisible);
              return true;
            }
          }
        }

        return false;
      }
    }
  }
}