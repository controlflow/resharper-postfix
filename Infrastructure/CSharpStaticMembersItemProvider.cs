using System.Drawing;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.Application.Settings;
using JetBrains.DocumentModel;
using JetBrains.ReSharper.ControlFlow.PostfixCompletion.Settings;
using JetBrains.ReSharper.Feature.Services.CodeCompletion;
using JetBrains.ReSharper.Feature.Services.CSharp.CodeCompletion.Infrastructure;
using JetBrains.ReSharper.Feature.Services.Lookup;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Impl;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.ExpectedTypes;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Resolve.Filters;
using JetBrains.ReSharper.Psi.Pointers;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Services;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.TextControl;
using JetBrains.Util;

namespace JetBrains.ReSharper.ControlFlow.PostfixCompletion
{
  // todo: show member signatures mode
  // todo: string instead of String (use R# setting)
  // todo: non-standard formatting of arguments
  // todo: decorate step - hide overriden signatures
  // todo: filter out extension methods
  // todo: double completion?
  // todo: parameter info
  // todo: caret pos

  [Language(typeof(CSharpLanguage))]
  public class CSharpStaticMembersItemProvider : CSharpItemsProviderBase<CSharpCodeCompletionContext>
  {
    protected override bool IsAvailable(CSharpCodeCompletionContext context)
    {
      var completionType = context.BasicContext.CodeCompletionType;
      return completionType == CodeCompletionType.AutomaticCompletion
          || completionType == CodeCompletionType.BasicCompletion;
    }

    protected override bool AddLookupItems(
      CSharpCodeCompletionContext context, GroupedItemsCollector collector)
    {
      var unterminatedContext = context.UnterminatedContext;
      if (unterminatedContext == null) return false;

      var expressionReference = unterminatedContext.Reference as IReferenceExpressionReference;
      if (expressionReference == null) return false;

      var referenceExpression = (IReferenceExpression) expressionReference.GetTreeNode();
      var qualifierExpression = referenceExpression.QualifierExpression;
      if (qualifierExpression == null) return false;

      var settingsStore = qualifierExpression.GetSettingsStore();
      if (!settingsStore.GetValue(PostfixSettingsAccessor.ShowStaticMembersInCodeCompletion))
        return false;

      var qualifierType = qualifierExpression.Type();
      if (!qualifierType.IsResolved) return false;

      var table = qualifierType.GetSymbolTable(context.PsiModule);
      var symbolTable = table
        .Filter(
          new Foo(qualifierType, referenceExpression.GetTypeConversionRule()),
          OverriddenFilter.INSTANCE,
          new AccessRightsFilter(new ElementAccessContext(qualifierExpression)));


      var itemsCollector = new GroupedItemsCollector();
      GetLookupItemsFromSymbolTable(symbolTable, itemsCollector, context, false);

      foreach (var lookupItem in itemsCollector.Items)
      {
        var t = lookupItem as DeclaredElementLookupItem;
        if (t != null)
        {
          t.TextColor = SystemColors.GrayText;
          SubscribeAfterComplete(t);

          collector.AddToBottom(t);
        }
      }

      return true;
    }

    private static void SubscribeAfterComplete([NotNull] DeclaredElementLookupItem lookupItem)
    {
      lookupItem.AfterComplete += (
        ITextControl textControl, ref TextRange range, ref TextRange decorationRange,
        TailType tailType, ref Suffix suffix, ref IRangeMarker marker) =>
      {
        var preferredDeclaredElement = lookupItem.PreferredDeclaredElement;
        if (preferredDeclaredElement == null) return;

        var method = (IMethod) preferredDeclaredElement.Element;
        var psiServices = lookupItem.Solution.GetPsiServices();
        psiServices.CommitAllDocuments();

        var referenceExpression = TextControlToPsi
          .GetElements<IReferenceExpression>(
            lookupItem.Solution, textControl.Document, range.StartOffset)
          .FirstOrDefault();

        if (referenceExpression == null) return;

        var qualifierText = referenceExpression.QualifierExpression.NotNull().GetText();
        var pointer = referenceExpression.CreateTreeElementPointer();

        var decRange = decorationRange.SetStartTo(range.EndOffset);

        textControl.Document.ReplaceText(
          TextRange.FromLength(decorationRange.EndOffset - (decRange.Length/2), 0),
          qualifierText);

        var containingType = method.GetContainingType().NotNull();
        var kw = CSharpTypeFactory.GetTypeKeyword(containingType.GetClrName());


        textControl.Document.ReplaceText(
          referenceExpression.QualifierExpression.GetDocumentRange().TextRange,
          kw ?? "T");

        psiServices.CommitAllDocuments();

        var eleme = pointer.GetTreeNode();
        if (eleme != null && kw == null)
        {
          var qualifierReference = (IReferenceExpression) eleme.QualifierExpression.NotNull();
          qualifierReference.Reference.BindTo(
            containingType, preferredDeclaredElement.Substitution);
        }
      };
    }

    sealed class Foo : SimpleSymbolFilter
    {
      private readonly IExpressionType myType;
      private readonly ICSharpTypeConversionRule myGetTypeConversionRule;

      public Foo(IExpressionType type, ICSharpTypeConversionRule getTypeConversionRule)
      {
        myType = type;
        myGetTypeConversionRule = getTypeConversionRule;
      }

      public override ResolveErrorType ErrorType
      {
        get { return ResolveErrorType.NOT_RESOLVED; }
      }

      public override bool Accepts(IDeclaredElement declaredElement, ISubstitution substitution)
      {
        var method = declaredElement as IMethod;
        if (method != null)
        {
          if (method.IsStatic && method.Parameters.Count > 0)
          {
            var parameter = method.Parameters[0];
            if (myType.IsImplicitlyConvertibleTo(parameter.Type, myGetTypeConversionRule))
              return true;
          }
        }

        return false;
      }
    }
  }
}