using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.Application;
using JetBrains.Application.Settings;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.ControlFlow.PostfixCompletion.LookupItems;
using JetBrains.ReSharper.ControlFlow.PostfixCompletion.Settings;
using JetBrains.ReSharper.Feature.Services.CodeCompletion;
using JetBrains.ReSharper.Feature.Services.CSharp.CodeCompletion.Infrastructure;
using JetBrains.ReSharper.Feature.Services.Lookup;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Pointers;
using JetBrains.ReSharper.Psi.Resources;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.Text;
using JetBrains.TextControl;
using JetBrains.UI.Icons;
using JetBrains.UI.RichText;
using JetBrains.Util;

namespace JetBrains.ReSharper.ControlFlow.PostfixCompletion
{
  [Language(typeof (CSharpLanguage))]
  public class CSharpEnumHelpersItemProvider : CSharpItemsProviderBase<CSharpCodeCompletionContext>
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
      var unterminated = context.UnterminatedContext;
      if (unterminated == null) return false;

      var reference = unterminated.Reference as IReferenceExpressionReference;
      if (reference == null) return false;

      var referenceExpression = (IReferenceExpression) reference.GetTreeNode();
      var qualifier = referenceExpression.QualifierExpression;
      if (qualifier == null) return false;

      // only on qualifiers of enumeration types
      var qualifierType = qualifier.Type() as IDeclaredType;
      if (qualifierType == null) return false;
      if (!qualifierType.IsResolved) return false;
      if (!qualifierType.IsEnumType()) return false;

      // disable helpers on constants and enumeration members itself
      var sourceReference = qualifier as IReferenceExpression;
      if (sourceReference != null)
      {
        var field = sourceReference.Reference.Resolve().DeclaredElement as IField;
        if (field != null && (field.IsConstant || field.IsEnumMember)) return false;
      }

      var settingsStore = qualifier.GetSettingsStore();
      if (!settingsStore.GetValue(PostfixSettingsAccessor.ShowEnumHelpersInCodeCompletion))
        return false;

      return AddEnumerationMembers(
        context, collector, qualifierType, referenceExpression);
    }

    [NotNull] private static readonly IClrTypeName 
      FlagsAttributeClrName = new ClrTypeName(typeof(FlagsAttribute).FullName);

    private static bool AddEnumerationMembers(
      [NotNull] CSharpCodeCompletionContext context, [NotNull] GroupedItemsCollector collector,
      [NotNull] IDeclaredType qualifierType, [NotNull] IReferenceExpression referenceExpression)
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
            ? GetBinaryRepresenation(convertible) : string.Empty;
          memberValues.Add(Pair.Of(member, memberValue));
        }
      }

      if (memberValues.Count == 0) return false;

      // create pointer to . in reference expression
      var maxLength = memberValues.Max(x => x.Second.Length);
      var reparsedDotRange = referenceExpression.Delimiter.GetTreeTextRange();
      var originalDotRange = context.UnterminatedContext.ToOriginalTreeRange(reparsedDotRange);
      var originalDotToken = context.BasicContext.File.FindTokenAt(originalDotRange.StartOffset);
      if (originalDotToken == null) return false;

      var pointer = originalDotToken.CreateTreeElementPointer();

      foreach (var member in memberValues)
      {
        var normalizedValue = member.Second.PadLeft(maxLength, '0');
        var value = isFlagsEnum ? normalizedValue : member.Second;

        var instance = new DeclaredElementInstance<IField>(member.First, substitution);
        var textLookupItem = new EnumMemberLookupItem(
          pointer, instance, normalizedValue, value, isFlagsEnum);

        collector.AddAtDefaultPlace(textLookupItem);
      }

      return true;
    }

    [NotNull]
    private static string GetBinaryRepresenation([NotNull] IConvertible convertible)
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

    private sealed class EnumMemberLookupItem : PostfixLookupItemBase, ILookupItem
    {
      [NotNull] private readonly ITreeNodePointer<ITreeNode> myDotPointer;
      [NotNull] private readonly IElementInstancePointer<IField> myMemberPointer;
      [NotNull] private readonly string myShortName;
      private readonly bool myIsFlags;

      public EnumMemberLookupItem(
        [NotNull] ITreeNodePointer<ITreeNode> dotPointer,
        [NotNull] DeclaredElementInstance<IField> enumMember,
        [NotNull] string normalizedValue,
        [NotNull] string value, bool isFlags)
      {
        myDotPointer = dotPointer;
        myMemberPointer = enumMember.CreateElementInstancePointer();
        myShortName = enumMember.Element.ShortName;
        myIsFlags = isFlags && normalizedValue.Any(x => x != '0');

        DisplayName = new RichText(myShortName, new TextStyle(FontStyle.Bold));
        Identity = "   ENUM_MEMBER_" + normalizedValue;

        if (value.Length <= 32) // protect from too heavy values
        {
          DisplayTypeName = new RichText("= " + value,
            new TextStyle(FontStyle.Regular, SystemColors.GrayText));
        }
      }

      public void Accept(ITextControl textControl,
        TextRange nameRange, LookupItemInsertType insertType,
        Suffix suffix, ISolution solution, bool keepCaretStill)
      {
        var services = solution.GetPsiServices();
        services.CommitAllDocuments();

        var enumMember = myMemberPointer.Resolve();
        if (enumMember == null) return;

        var dotToken = myDotPointer.GetTreeNode();
        if (dotToken == null) return;

        var referenceExpression = dotToken.Parent as IReferenceExpression;
        if (referenceExpression == null) return;

        var factory = CSharpElementFactory.GetInstance(dotToken);
        var template = myIsFlags ? "($0 & $1) != 0" : "$0 == $1";
        var enumeMemberCheck = factory.CreateExpression(
          template, referenceExpression.QualifierExpression, enumMember);

        services.DoTransaction(typeof(CSharpEnumHelpersItemProvider).FullName, () =>
        {
          using (WriteLockCookie.Create())
            referenceExpression.ReplaceBy(enumeMemberCheck);
        });
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

      public string OrderingString { get { return Identity; } }
      public string Identity { get; private set; }
    }
  }
}