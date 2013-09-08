using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.Application;
using JetBrains.DocumentModel;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.ControlFlow.PostfixCompletion.LookupItems;
using JetBrains.ReSharper.Feature.Services.CodeCompletion;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.Hotspots;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.LiveTemplates;
using JetBrains.ReSharper.Feature.Services.Lookup;
using JetBrains.ReSharper.LiveTemplates;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.TextControl;
using JetBrains.Util;
#if RESHARPER7
using JetBrains.ReSharper.Feature.Services.CSharp.LiveTemplates;
#elif RESHARPER8
using JetBrains.ReSharper.Feature.Services.LiveTemplates.Macros.Implementations;
#endif

namespace JetBrains.ReSharper.ControlFlow.PostfixCompletion.TemplateProviders
{
  [PostfixTemplateProvider(
    templateNames: new[] { "parse", "tryparse" },
    description: "Parses string as value of some type",
    example: "int.Parse(expr)")]
  public class ParseStringTemplateProvider : IPostfixTemplateProvider
  {
    public void CreateItems(
      PostfixTemplateAcceptanceContext context, ICollection<ILookupItem> consumer)
    {
      foreach (var exprContext in context.Expressions)
      {
        var type = exprContext.Type;
        if (type.IsResolved && type.IsString())
        {
          consumer.Add(new LookupItem("parse",
            exprContext, context.LookupItemsOwner, false));
          consumer.Add(new LookupItem("tryParse",
            exprContext, context.LookupItemsOwner, true));
          break;
        }
      }
    }

    private sealed class LookupItem : ExpressionPostfixLookupItem<IInvocationExpression>
    {
      private readonly bool myIsTryParse;
      [NotNull] private readonly ILookupItemsOwner myLookupItemsOwner;

      public LookupItem([NotNull] string shortcut,
        [NotNull] PrefixExpressionContext context,
        [NotNull] ILookupItemsOwner lookupItemsOwner,
        bool isTryParse) : base(shortcut, context)
      {
        myLookupItemsOwner = lookupItemsOwner;
        myIsTryParse = isTryParse;
      }

      protected override IInvocationExpression CreateExpression(
        CSharpElementFactory factory, ICSharpExpression expression)
      {
        var template = myIsTryParse ? "int.TryParse($0, )" : "int.Parse($0)";
        return (IInvocationExpression) factory.CreateExpression(template, expression);
      }

      protected override void AfterComplete(ITextControl textControl,
        Suffix suffix, IInvocationExpression expression, int? caretPosition)
      {
        var parseReference = (IReferenceExpression) expression.InvokedExpression;
        var typeQualifier = parseReference.QualifierExpression.NotNull();

        var solution = expression.GetSolution();
        var psiModule = typeQualifier.GetPsiModule();

        var typesWithParsers = GetTypesWithParsers(typeQualifier);
        var templateExpression =
#if RESHARPER7
          new CSharpTemplateUtil.TypeTemplateExpression(
            "int", typesWithParsers.ToArray(), typesWithParsers[0],
#elif RESHARPER8
          new TypeTemplateExpression(typesWithParsers,
#endif
          psiModule, CSharpLanguage.Instance);

        var hotspotInfo = new HotspotInfo(
          new TemplateField("type", templateExpression, 0),
          typeQualifier.GetDocumentRange().GetHotspotRange());

        var argumentsRange = expression.ArgumentList.GetDocumentRange();
        var endSelectionRange = argumentsRange.EndOffsetRange().TextRange;

        var session = LiveTemplatesManager.Instance.CreateHotspotSessionAtopExistingText(
          solution, endSelectionRange, textControl,
          LiveTemplatesManager.EscapeAction.LeaveTextAndCaret, new[] { hotspotInfo });

        // paranthesis marker for parameter info
        var marker = expression.LPar.GetDocumentRange()
          .SetEndTo(expression.RPar.GetDocumentRange().TextRange.EndOffset)
          .CreateRangeMarker();

        session.AdviceFinished((_, terminationType) =>
        {
          if (myIsTryParse)
          {
            Shell.Instance.Locks.QueueReadLock(
              "Smart completion for .tryparse", () =>
              {
                var manager = solution.GetComponent<IntellisenseManager>();
                manager.ExecuteManualCompletion(
                  CodeCompletionType.SmartCompletion,
                  textControl, solution, EmptyAction.Instance,
#if RESHARPER8
                  manager.GetPrimaryEvaluationMode(CodeCompletionType.SmartCompletion),
#endif
                  AutocompletionBehaviour.DoNotAutocomplete);
              });
          }

          if (marker.IsValid)
          {
            LookupUtil.ShowParameterInfo(
              solution, textControl, marker.Range, null, myLookupItemsOwner);
          }
        });

        session.Execute();
      }
    }

    [NotNull] private static IList<IType> GetTypesWithParsers([NotNull] ITreeNode context)
    {
#if RESHARPER7
      var cacheManager = context.GetPsiServices().CacheManager;
      var symbolScope = cacheManager.GetDeclarationsCache(context.GetPsiModule(), true, true);
#elif RESHARPER8
      var symbolCache = context.GetPsiServices().Symbols;
      var symbolScope = symbolCache.GetSymbolScope(
        context.GetPsiModule(), context.GetResolveContext(), true, true);
#endif

      var list = new LocalList<IType>();
      foreach (var type in TypesWithParsers)
      {
        var typeElement = symbolScope.GetTypeElementByCLRName(type.FullName);
        if (typeElement == null) continue;

        list.Add(TypeFactory.CreateType(typeElement, typeElement.IdSubstitution));
      }

      return list.ResultingList();
    }

    [NotNull] private static readonly Type[] TypesWithParsers =
    {
      typeof(int),
      typeof(byte),
      typeof(sbyte),
      typeof(char),
      typeof(short),
      typeof(long),
      typeof(ushort),
      typeof(uint),
      typeof(ulong),
      typeof(decimal),
      typeof(double),
      typeof(float),
      typeof(TimeSpan),
      typeof(DateTime),
      typeof(DateTimeOffset),
      typeof(Version),
      typeof(Guid),
    };
  }
}