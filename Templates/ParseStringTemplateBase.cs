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
using JetBrains.ReSharper.Feature.Services.LiveTemplates.Macros.Implementations;
using JetBrains.ReSharper.Feature.Services.Lookup;
using JetBrains.ReSharper.LiveTemplates;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.TextControl;
using JetBrains.Util;

namespace JetBrains.ReSharper.ControlFlow.PostfixCompletion.Templates
{
  public abstract class ParseStringTemplateBase
  {
    protected sealed class ParseLookupItem : ExpressionPostfixLookupItem<IInvocationExpression>
    {
      private readonly bool myIsTryParse;
      [NotNull] private readonly ILookupItemsOwner myLookupItemsOwner;
      [NotNull] private readonly LiveTemplatesManager myTemplatesManager;
      [NotNull] private readonly IShellLocks myLocks;

      public ParseLookupItem(
        [NotNull] string shortcut, [NotNull] PrefixExpressionContext context,
        [NotNull] LiveTemplatesManager templatesManager, [NotNull] IShellLocks shellLocks,
        [NotNull] ILookupItemsOwner lookupItemsOwner, bool isTryParse)
        : base(shortcut, context)
      {
        myLookupItemsOwner = lookupItemsOwner;
        myIsTryParse = isTryParse;
        myTemplatesManager = templatesManager;
        myLocks = shellLocks;
      }

      protected override IInvocationExpression CreateExpression(
        CSharpElementFactory factory, ICSharpExpression expression)
      {
        var template = myIsTryParse ? "int.TryParse($0, )" : "int.Parse($0)";
        return (IInvocationExpression) factory.CreateExpression(template, expression);
      }

      protected override void AfterComplete(
        ITextControl textControl, Suffix suffix, IInvocationExpression expression, int? caretPosition)
      {
        var parseReference = (IReferenceExpression) expression.InvokedExpression;
        var typeQualifier = parseReference.QualifierExpression.NotNull();

        var solution = expression.GetSolution();
        var psiModule = typeQualifier.GetPsiModule();

        var typesWithParsers = GetTypesWithParsers(typeQualifier);
        var templateExpression = new TypeTemplateExpression(
          typesWithParsers, psiModule, CSharpLanguage.Instance);

        var hotspotInfo = new HotspotInfo(
          new TemplateField("type", templateExpression, 0),
          typeQualifier.GetDocumentRange().GetHotspotRange());

        var argumentsRange = expression.ArgumentList.GetDocumentRange();
        var endSelectionRange = argumentsRange.EndOffsetRange().TextRange;

        var session = myTemplatesManager.CreateHotspotSessionAtopExistingText(
          solution, endSelectionRange, textControl,
          LiveTemplatesManager.EscapeAction.LeaveTextAndCaret, new[] {hotspotInfo});

        // paranthesis marker for parameter info
        var marker = expression.LPar.GetDocumentRange()
          .SetEndTo(expression.RPar.GetDocumentRange().TextRange.EndOffset)
          .CreateRangeMarker();

        session.AdviceFinished((_, terminationType) =>
        {
          if (myIsTryParse)
          {
            myLocks.QueueReadLock("Smart completion for .tryparse", () =>
            {
              var manager = solution.GetComponent<IntellisenseManager>();
              manager.ExecuteManualCompletion(
                CodeCompletionType.SmartCompletion,
                textControl, solution, EmptyAction.Instance,
                manager.GetPrimaryEvaluationMode(CodeCompletionType.SmartCompletion),
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

    [NotNull]
    private static IList<IType> GetTypesWithParsers([NotNull] ITreeNode context)
    {
      var symbolCache = context.GetPsiServices().Symbols;
      var symbolScope = symbolCache.GetSymbolScope(
        context.GetPsiModule(), context.GetResolveContext(), true, true);

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
      typeof(Guid)
    };
  }
}