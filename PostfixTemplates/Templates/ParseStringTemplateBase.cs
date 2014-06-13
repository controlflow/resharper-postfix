using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.Application;
using JetBrains.Application.Settings;
using JetBrains.DocumentModel;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.CodeCompletion;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.Hotspots;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.LiveTemplates;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.Macros.Implementations;
using JetBrains.ReSharper.Feature.Services.Lookup;
using JetBrains.ReSharper.PostfixTemplates.LookupItems;
using JetBrains.ReSharper.PostfixTemplates.Settings;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.TextControl;
using JetBrains.Util;
#if RESHARPER8
using JetBrains.ReSharper.LiveTemplates;
#elif RESHARPER9
using JetBrains.ReSharper.Resources.Shell;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.Templates;
#endif

namespace JetBrains.ReSharper.PostfixTemplates.Templates
{
  public abstract class ParseStringTemplateBase
  {
    protected sealed class ParseItem : ExpressionPostfixLookupItem<IInvocationExpression>
    {
      [NotNull] private readonly ILookupItemsOwner myLookupItemsOwner;
      [NotNull] private readonly LiveTemplatesManager myTemplatesManager;
      private readonly bool myIsTryParse;

      public ParseItem([NotNull] string shortcut,
                       [NotNull] PrefixExpressionContext context,
                       bool isTryParse)
        : base(shortcut, context)
      {
        myIsTryParse = isTryParse;

        var executionContext = context.PostfixContext.ExecutionContext;
        myTemplatesManager = executionContext.LiveTemplatesManager;
        myLookupItemsOwner = executionContext.LookupItemsOwner;
      }

      protected override IInvocationExpression CreateExpression(CSharpElementFactory factory,
                                                                ICSharpExpression expression)
      {
        var template = myIsTryParse ? "int.TryParse($0, )" : "int.Parse($0)";
        return (IInvocationExpression) factory.CreateExpression(template, expression);
      }

      protected override void AfterComplete(ITextControl textControl, IInvocationExpression expression)
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
          typeQualifier.GetDocumentRange());

        var argumentsRange = expression.ArgumentList.GetDocumentRange();

        var session = myTemplatesManager.CreateHotspotSessionAtopExistingText(
          solution, argumentsRange.EndOffsetRange().TextRange, textControl,
          LiveTemplatesManager.EscapeAction.LeaveTextAndCaret, hotspotInfo);

        // parentheses marker for parameter info
        var marker = expression.LPar.GetDocumentRange()
          .SetEndTo(expression.RPar.GetDocumentRange().TextRange.EndOffset)
          .CreateRangeMarker();

        var settingsStore = expression.GetSettingsStore();
        var invokeParameterName = settingsStore.GetValue(PostfixSettingsAccessor.InvokeParameterInfo);

        session.Closed.Advise(Lifetime, args =>
        {
          if (myIsTryParse)
          {
            solution.GetComponent<IShellLocks>().QueueReadLock("Smart completion for .tryparse", () =>
            {
#if RESHARPER8
              var intellisenseManager = solution.GetComponent<IntellisenseManager>();
#elif RESHARPER9
              var intellisenseManager = solution.GetComponent<ICodeCompletionSessionManager>();
#endif
              intellisenseManager.ExecuteManualCompletion(
                CodeCompletionType.SmartCompletion, textControl, solution, EmptyAction.Instance,
                intellisenseManager.GetPrimaryEvaluationMode(CodeCompletionType.SmartCompletion),
                AutocompletionBehaviour.DoNotAutocomplete);
            });
          }

          if (invokeParameterName)
          {
            using (ReadLockCookie.Create())
            {
              if (marker.IsValid)
              {
                LookupUtil.ShowParameterInfo(
                  solution, textControl, marker.Range, null, myLookupItemsOwner);
              }
            }
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
        context.GetPsiModule(), context.GetResolveContext(),
        withReferences: true, caseSensitive: true);

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