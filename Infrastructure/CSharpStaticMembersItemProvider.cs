using System;
using System.Drawing;
using System.Linq;
using JetBrains.DocumentModel;
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
using JetBrains.ReSharper.Psi.Resolve.TypeInference;
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

      var expressionReference = unterminatedContext.Reference as IReferenceExpressionReference;
      if (expressionReference != null)
      {
        var referenceExpression = (IReferenceExpression) expressionReference.GetTreeNode();
        var qualifier = referenceExpression.QualifierExpression;
        if (qualifier != null)
        {
          var type = qualifier.Type();
          if (type.IsResolved)
          {
            var table = type.GetSymbolTable(context.PsiModule);
            var symbolInfos = table
              .Filter(
                new Foo(type, referenceExpression.GetTypeConversionRule()),
                OverriddenFilter.INSTANCE,
                new AccessRightsFilter(new ElementAccessContext(qualifier)))
              .GetAllSymbolInfos();

            var allElements = new OneToListMap<string, DeclaredElementInstance<IMethod>>();
            foreach (var symbolInfo in symbolInfos)
            {
              var element = (IMethod)symbolInfo.GetDeclaredElement();
              if (!TypeInferenceUtil.TypeParametersAreInferrable(element)) continue;

              var instance = new DeclaredElementInstance<IMethod>(element, symbolInfo.GetSubstitution());
              allElements.Add(symbolInfo.ShortName, instance);
            }

            var solution = context.BasicContext.Solution;
            foreach (var pair in allElements)
            {
              var item = context.LookupItemsFactory.CreateMethodsLookupItem(pair.Key, pair.Value, true);

              item.TextColor = SystemColors.GrayText;

              item.AfterComplete +=
                (ITextControl control, ref TextRange range,
                  ref TextRange decorationRange, TailType tailType,
                  ref Suffix suffix, ref IRangeMarker marker) =>
                {
                  var method = (IMethod) item.PreferredDeclaredElement.Element;
                  var psiServices = solution.GetPsiServices();

                  psiServices.CommitAllDocuments();
                  var refExpr = TextControlToPsi
                    .GetElements<IReferenceExpression>(item.Solution, control.Document, range.StartOffset)
                    .ToList().FirstOrDefault();

                  var qualifierText = refExpr.QualifierExpression.GetText();
                  var pointer = refExpr.CreateTreeElementPointer();

                  var decRange = decorationRange.SetStartTo(range.EndOffset);

                  control.Document.ReplaceText(
                    TextRange.FromLength(decorationRange.EndOffset - (decRange.Length / 2), 0),
                    qualifierText);

                  control.Document.ReplaceText(
                    refExpr.QualifierExpression.GetDocumentRange().TextRange, "T");

                  psiServices.CommitAllDocuments();

                  var eleme = pointer.GetTreeNode();
                  if (eleme != null)
                  {
                    var re = (IReferenceExpression) eleme.QualifierExpression;
                    re.Reference.BindTo(
                      method.GetContainingType(),
                      item.PreferredDeclaredElement.Substitution);
                  }

                  
                  GC.KeepAlive(this);
                };

              collector.AddAtDefaultPlace(item);
            }
          }
        }

        
      }

      return false;
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