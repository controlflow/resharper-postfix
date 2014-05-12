using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.Application;
using JetBrains.Application.Settings;
using JetBrains.DocumentModel;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.CodeCompletion;
using JetBrains.ReSharper.Feature.Services.CSharp.CodeCompletion.Infrastructure;
using JetBrains.ReSharper.Feature.Services.Lookup;
using JetBrains.ReSharper.Feature.Services.Tips;
using JetBrains.ReSharper.PostfixTemplates.LookupItems;
using JetBrains.ReSharper.PostfixTemplates.Settings;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Parsing;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Pointers;
using JetBrains.ReSharper.Psi.Resources;
using JetBrains.ReSharper.Psi.Services;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.Text;
using JetBrains.TextControl;
using JetBrains.UI.Icons;
using JetBrains.UI.RichText;
using JetBrains.Util;
#if RESHARPER9
using JetBrains.ReSharper.Features.Intellisense.CodeCompletion.CSharp.Rules;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.LookupItems;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.Match;
#endif

namespace JetBrains.ReSharper.PostfixTemplates.CodeCompletion
{
  [Language(typeof(CSharpLanguage))]
  public class CSharpEnumCaseItemProvider : CSharpItemsProviderBase<CSharpCodeCompletionContext>
  {
    protected override bool IsAvailable(CSharpCodeCompletionContext context)
    {
      var completionType = context.BasicContext.CodeCompletionType;
      return completionType == CodeCompletionType.AutomaticCompletion
          || completionType == CodeCompletionType.BasicCompletion;
    }

    protected override bool AddLookupItems(CSharpCodeCompletionContext context,
                                           GroupedItemsCollector collector)
    {
      var referenceExpression = context.UnterminatedContext.ToReferenceExpression() ??
                                context.TerminatedContext.ToReferenceExpression();
      if (referenceExpression == null) return false;

      var qualifier = referenceExpression.QualifierExpression;
      if (qualifier == null) return false;

      var settingsStore = qualifier.GetSettingsStore();
      if (!settingsStore.GetValue(PostfixSettingsAccessor.ShowEnumHelpers))
        return false;

      // only on qualifiers of enumeration types
      var qualifierType = qualifier.Type() as IDeclaredType;
      if (qualifierType == null || !qualifierType.IsResolved) return false;

      if (qualifierType.IsNullable()) // unwrap from nullable type
      {
        qualifierType = qualifierType.GetNullableUnderlyingType() as IDeclaredType;
        if (qualifierType == null || !qualifierType.IsResolved) return false;
      }

      if (!qualifierType.IsEnumType()) return false;

      // disable helpers on constants and enumeration members itself
      var sourceReference = qualifier as IReferenceExpression;
      if (sourceReference != null)
      {
        var field = sourceReference.Reference.Resolve().DeclaredElement as IField;
        if (field != null && (field.IsConstant || field.IsEnumMember)) return false;

        // value member can clash with enum type name
        if (sourceReference.QualifierExpression == null &&
            sourceReference.NameIdentifier.Name == qualifierType.GetClrName().ShortName)
        {
          foreach (var lookupItem in collector.Items)
          {
            var elementInstance = lookupItem.GetDeclaredElement();
            if (elementInstance == null) continue;

            var enumMember = elementInstance.Element as IField;
            if (enumMember != null && enumMember.IsEnumMember)
              return false;
          }
        }
      }

      return AddEnumerationMembers(
        context, collector, qualifierType, referenceExpression);
    }

    [NotNull] private static readonly IClrTypeName FlagsAttributeClrName =
      new ClrTypeName(typeof(FlagsAttribute).FullName);

    private static bool AddEnumerationMembers([NotNull] CSharpCodeCompletionContext context,
                                              [NotNull] GroupedItemsCollector collector,
                                              [NotNull] IDeclaredType qualifierType,
                                              [NotNull] IReferenceExpression referenceExpression)
    {
      var enumerationType = (IEnum) qualifierType.GetTypeElement().NotNull();
      var substitution = qualifierType.GetSubstitution();
      var memberValues = new List<Pair<IField, string>>();

      var isFlagsEnum = enumerationType.HasAttributeInstance(FlagsAttributeClrName, false);
      if (!isFlagsEnum)
      {
        foreach (var member in enumerationType.EnumMembers)
        {
          var formattable = member.ConstantValue.Value as IFormattable;
          var memberValue = (formattable != null)
            ? formattable.ToString("D", CultureInfo.InvariantCulture) : string.Empty;
          memberValues.Add(Pair.Of(member, memberValue));
        }
      }
      else
      {
        foreach (var member in enumerationType.EnumMembers)
        {
          var convertible = member.ConstantValue.Value as IConvertible;
          var memberValue = (convertible != null)
            ? GetBinaryRepresentation(convertible) : string.Empty;
          memberValues.Add(Pair.Of(member, memberValue));
        }
      }

      if (memberValues.Count == 0) return false;

      // create pointer to . in reference expression
      var maxLength = memberValues.Max(x => x.Second.Length);
      var reparsedDotRange = referenceExpression.Delimiter.GetTreeTextRange();
      var originalDotRange = context.UnterminatedContext.ToOriginalTreeRange(reparsedDotRange);
      var file = context.BasicContext.File;
      var dotMarker = file.GetDocumentRange(originalDotRange).CreateRangeMarker();

      foreach (var member in memberValues)
      {
        var normalizedValue = member.Second.PadLeft(maxLength, '0');
        var value = isFlagsEnum ? normalizedValue : member.Second;

        var instance = new DeclaredElementInstance<IField>(member.First, substitution);
        var textLookupItem = new EnumMemberLookupItem(
          dotMarker, instance, normalizedValue, value, isFlagsEnum);

        collector.AddAtDefaultPlace(textLookupItem);
      }

      return true;
    }

    [NotNull]
    private static string GetBinaryRepresentation([NotNull] IConvertible convertible)
    {
      switch (convertible.GetTypeCode())
      {
        case TypeCode.SByte:  return Convert.ToString((sbyte) convertible, 2);
        case TypeCode.Byte:   return Convert.ToString((byte) convertible, 2);
        case TypeCode.Int16:  return Convert.ToString((short) convertible, 2);
        case TypeCode.UInt16: return Convert.ToString((ushort) convertible, 2);
        case TypeCode.Int32:  return Convert.ToString((int) convertible, 2);
        case TypeCode.UInt32: return Convert.ToString((uint) convertible, 2);
        case TypeCode.Int64:  return Convert.ToString((long) convertible, 2);
        case TypeCode.UInt64: return Convert.ToString((long)(ulong) convertible, 2);
      }

      return string.Empty;
    }

    private sealed class EnumMemberLookupItem : PostfixLookupItemBase,
      // ReSharper disable RedundantNameQualifier
      JetBrains.ReSharper.Feature.Services.Lookup.ILookupItem
      // ReSharper enable RedundantNameQualifier
    {
      [NotNull] private readonly IRangeMarker myDotRangeMarker;
      [NotNull] private readonly IElementInstancePointer<IField> myPointer;
      [NotNull] private readonly string myShortName;
      private readonly bool myIsFlags;

      public EnumMemberLookupItem([NotNull] IRangeMarker dotRangeMarker,
                                  [NotNull] DeclaredElementInstance<IField> enumMember,
                                  [NotNull] string normalizedValue,
                                  [NotNull] string value, bool isFlags)
      {
        myDotRangeMarker = dotRangeMarker;
        myPointer = enumMember.CreateElementInstancePointer();
        myShortName = enumMember.Element.ShortName;
        myIsFlags = isFlags && normalizedValue.Any(x => x != '0'); // ugh :(

        DisplayName = new RichText(myShortName, new TextStyle(FontStyle.Bold));
        Identity = "   ENUM_MEMBER_" + normalizedValue;

        if (value.Length <= 32) // protect from too heavy values
        {
          DisplayTypeName = new RichText("= " + value,
            new TextStyle(FontStyle.Regular, SystemColors.GrayText));
        }
      }

      public void Accept(ITextControl textControl, TextRange nameRange,
                         LookupItemInsertType insertType, Suffix suffix,
                         ISolution solution, bool keepCaretStill)
      {
        textControl.Document.ReplaceText(nameRange, "E()");

        var psiServices = solution.GetPsiServices();
        psiServices.CommitAllDocuments();

        var enumMember = myPointer.Resolve();
        if (enumMember == null) return;

        var referenceExpression = FindReferenceExpression(textControl, solution);
        var invocation = InvocationExpressionNavigator.GetByInvokedExpression(referenceExpression);
        if (invocation == null) return;

        TipsManager.Instance.FeatureIsUsed(
          "Plugin.ControlFlow.PostfixTemplates.<enum>", textControl.Document, solution);

        var factory = CSharpElementFactory.GetInstance(referenceExpression);
        var template = myIsFlags ? "($0 & $1) != 0" : "$0 == $1";
        var enumMemberCheck = factory.CreateExpression(
          template, referenceExpression.QualifierExpression, enumMember);

        var commandName = typeof(CSharpEnumCaseItemProvider).FullName;
        var caretPointer = psiServices.DoTransaction(commandName, () =>
        {
          using (WriteLockCookie.Create())
          {
            var memberCheck = invocation.ReplaceBy(enumMemberCheck);
            return memberCheck.CreatePointer();
          }
        });

        var checkExpression = caretPointer.GetTreeNode();
        if (checkExpression != null)
        {
          var offset = checkExpression.GetDocumentRange().TextRange.EndOffset;
          textControl.Caret.MoveTo(offset, CaretVisualPlacement.DontScrollIfVisible);
        }
      }

      [CanBeNull]
      private IReferenceExpression FindReferenceExpression([NotNull] ITextControl textControl,
                                                           [NotNull] ISolution solution)
      {
        var dotRange = myDotRangeMarker.DocumentRange;
        if (!dotRange.IsValid()) return null;

        var tokenOffset = dotRange.TextRange.StartOffset;
        foreach (var token in TextControlToPsi
          .GetElements<ITokenNode>(solution, textControl.Document, tokenOffset))
        {
          if (token.GetTokenType() == CSharpTokenType.DOT)
          {
            var expression = token.Parent as IReferenceExpression;
            if (expression != null) return expression;
          }
        }

        return null;
      }

      public MatchingResult Match(string prefix, ITextControl textControl)
      {
        return LookupUtil.MatchPrefix(new IdentifierMatcher(prefix), myShortName);
      }

      public IconId Image
      {
        get { return PsiSymbolsThemedIcons.EnumMember.Id; }
      }

      public RichText DisplayName { get; private set; }
      public RichText DisplayTypeName { get; private set; }

      // ReSharper disable once UnusedMember.Local
      public string OrderingString
      {
        get { return Identity; }
      }

      public string Identity { get; private set; }
    }
  }
}