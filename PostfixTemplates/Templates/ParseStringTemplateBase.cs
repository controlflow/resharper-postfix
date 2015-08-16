using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.Application;
using JetBrains.Application.Settings;
using JetBrains.DataFlow;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.CodeCompletion;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.Hotspots;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.LiveTemplates;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.Macros.Implementations;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.Templates;
using JetBrains.ReSharper.Feature.Services.Lookup;
using JetBrains.ReSharper.PostfixTemplates.Contexts.CSharp;
using JetBrains.ReSharper.PostfixTemplates.LookupItems;
using JetBrains.ReSharper.PostfixTemplates.Settings;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Resources.Shell;
using JetBrains.TextControl;
using JetBrains.Util;

namespace JetBrains.ReSharper.PostfixTemplates.Templates
{
  public abstract class ParseStringTemplateBase
  {
    protected sealed class ParseItem : ExpressionPostfixLookupItem<IInvocationExpression>
    {
      [NotNull] private readonly ILookupItemsOwner myLookupItemsOwner;
      [NotNull] private readonly LiveTemplatesManager myTemplatesManager;
      private readonly bool myIsTryParse;

      public ParseItem([NotNull] string shortcut, [NotNull] CSharpPostfixExpressionContext context, bool isTryParse)
        : base(shortcut, context)
      {
        myIsTryParse = isTryParse;

        var executionContext = context.PostfixContext.ExecutionContext;
        myTemplatesManager = executionContext.LiveTemplatesManager;
        myLookupItemsOwner = executionContext.LookupItemsOwner;
      }

      protected override IInvocationExpression CreateExpression(CSharpElementFactory factory, ICSharpExpression expression)
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
        var templateExpression = new TypeTemplateExpression(typesWithParsers, psiModule, CSharpLanguage.Instance);
        var templateField = new TemplateField("type", templateExpression, 0);

        var hotspotInfo = new HotspotInfo(templateField, typeQualifier.GetDocumentRange());

        var argumentsRange = expression.ArgumentList.GetDocumentRange();

        var endSelectionRange = argumentsRange.EndOffsetRange().TextRange;
        var session = myTemplatesManager.CreateHotspotSessionAtopExistingText(
          solution, endSelectionRange, textControl, LiveTemplatesManager.EscapeAction.LeaveTextAndCaret, hotspotInfo);

        var settingsStore = expression.GetSettingsStore();
        var invokeParameterInfo = settingsStore.GetValue(PostfixSettingsAccessor.InvokeParameterInfo);

        session.Closed.Advise(EternalLifetime.Instance, args =>
        {
          if (myIsTryParse)
          {
            var shellLocks = solution.GetComponent<IShellLocks>();
            shellLocks.QueueReadLock("Smart completion for .tryparse", () =>
            {
              var intellisenseManager = solution.GetComponent<ICodeCompletionSessionManager>();
              intellisenseManager.ExecuteManualCompletion(
                CodeCompletionType.SmartCompletion, textControl, solution, EmptyAction.Instance,
                intellisenseManager.GetPrimaryEvaluationMode(CodeCompletionType.SmartCompletion),
                AutocompletionBehaviour.DoNotAutocomplete);
            });
          }

          if (invokeParameterInfo)
          {
            using (ReadLockCookie.Create())
            {
              LookupUtil.ShowParameterInfo(solution, textControl, myLookupItemsOwner);
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
        context.GetPsiModule(), context.GetResolveContext(), withReferences: true, caseSensitive: true);

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