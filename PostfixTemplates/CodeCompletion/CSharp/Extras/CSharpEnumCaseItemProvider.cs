using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.Application.Settings;
using JetBrains.DocumentModel;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.CodeCompletion;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.LookupItems;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.Match;
using JetBrains.ReSharper.Feature.Services.CSharp.CodeCompletion.Infrastructure;
using JetBrains.ReSharper.Feature.Services.Lookup;
using JetBrains.ReSharper.Feature.Services.Tips;
using JetBrains.ReSharper.Feature.Services.Util;
using JetBrains.ReSharper.Features.Intellisense.CodeCompletion.CSharp.Rules;
using JetBrains.ReSharper.PostfixTemplates.LookupItems;
using JetBrains.ReSharper.PostfixTemplates.Settings;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Impl.Resolve;
using JetBrains.ReSharper.Psi.CSharp.Parsing;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Resolve.Managed;
using JetBrains.ReSharper.Psi.Pointers;
using JetBrains.ReSharper.Psi.Resources;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.ReSharper.Resources.Shell;
using JetBrains.TextControl;
using JetBrains.UI.Icons;
using JetBrains.UI.RichText;
using JetBrains.Util;

namespace JetBrains.ReSharper.PostfixTemplates.CodeCompletion.CSharp
{
  [Language(typeof(CSharpLanguage))]
  public class CSharpEnumCaseItemProvider : CSharpItemsProviderBase<CSharpCodeCompletionContext>
  {
    protected override bool IsAvailable(CSharpCodeCompletionContext context)
    {
      return context.BasicContext.CodeCompletionType == CodeCompletionType.BasicCompletion;
    }

    protected override bool AddLookupItems(CSharpCodeCompletionContext context, GroupedItemsCollector collector)
    {
      var referenceExpression = context.UnterminatedContext.ToReferenceExpression() ??
                                context.TerminatedContext.ToReferenceExpression();
      if (referenceExpression == null) return false;

      var qualifierExpression = referenceExpression.QualifierExpression;
      if (qualifierExpression == null) return false;

      var settingsStore = qualifierExpression.GetSettingsStore();
      if (!settingsStore.GetValue(PostfixTemplatesSettingsAccessor.ShowEnumHelpers)) return false;

      var qualifierType = GetEnumerationExpressionType(qualifierExpression);
      if (qualifierType == null) return false;

      var sourceReference = qualifierExpression as IReferenceExpression;
      if (sourceReference != null)
      {
        if (IsInvokedOverContantValue(sourceReference)) return false;

        if (sourceReference.QualifierExpression == null &&
            IsInvokedOverPropertyOrClassReference(sourceReference)) return false;
      }

      return AddEnumerationMembers(context, collector, qualifierType, referenceExpression);
    }

    [CanBeNull]
    private static IDeclaredType GetEnumerationExpressionType([NotNull] ICSharpExpression expression)
    {
      var qualifierType = expression.Type() as IDeclaredType;
      if (qualifierType == null) return null;

      if (!qualifierType.IsResolved) return null;

      if (qualifierType.IsNullable()) // unwrap from nullable type
      {
        qualifierType = qualifierType.GetNullableUnderlyingType() as IDeclaredType;
        if (qualifierType == null) return null;

        if (!qualifierType.IsResolved) return null;
      }

      return qualifierType.IsEnumType() ? qualifierType : null;
    }

    private static bool IsInvokedOverContantValue([NotNull] IReferenceExpression reference)
    {
      var resolveResult = reference.Reference.Resolve();

      var field = resolveResult.DeclaredElement as IField;
      if (field == null) return false;

      return field.IsConstant || field.IsEnumMember;
    }

    private static bool IsInvokedOverPropertyOrClassReference([NotNull] IReferenceExpression expression)
    {
      var managedTwoPhaseReference = expression.Reference as IManagedTwoPhaseReferenceImpl;
      if (managedTwoPhaseReference == null) return false;

      var preResolveResult = managedTwoPhaseReference.CurrentPreResolveResult; // ewww
      if (preResolveResult == null) return false;

      return preResolveResult.Result is PropertyOrClassPartialResult;
    }

    private static bool AddEnumerationMembers(
      [NotNull] CSharpCodeCompletionContext context, [NotNull] GroupedItemsCollector collector,
      [NotNull] IDeclaredType qualifierType, [NotNull] IReferenceExpression referenceExpression)
    {
      // todo: missing substitutions!

      var enumerationType = (IEnum) qualifierType.GetTypeElement().NotNull();
      var substitution = qualifierType.GetSubstitution();
      var memberValues = new List<Pair<IField, string>>();

      // todo: delay this to presentation!
      var isFlagsEnum = enumerationType.HasAttributeInstance(PredefinedType.FLAGS_ATTRIBUTE_CLASS, false);
      if (!isFlagsEnum)
      {
        foreach (var enumMember in enumerationType.EnumMembers)
        {
          var formattable = enumMember.ConstantValue.Value as IFormattable;
          var memberValue = (formattable != null) ? formattable.ToString("D", CultureInfo.InvariantCulture) : string.Empty;
          memberValues.Add(Pair.Of(enumMember, memberValue));
        }
      }
      else
      {
        foreach (var enumMember in enumerationType.EnumMembers)
        {
          var convertible = enumMember.ConstantValue.Value as IConvertible;
          var memberValue = (convertible != null) ? GetBinaryRepresentation(convertible) : string.Empty;
          memberValues.Add(Pair.Of(enumMember, memberValue));
        }
      }

      if (memberValues.Count == 0) return false;

      // create pointer to . in reference expression
      // todo: check with C# 6.0 ?.
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
        var textLookupItem = new EnumMemberLookupItem(dotMarker, instance, normalizedValue, value, isFlagsEnum);

        collector.Add(textLookupItem);
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

    private sealed class EnumMemberLookupItem : PostfixLookupItemBase, ILookupItem
    {
      [NotNull] private readonly string myIdentity;
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
        myIdentity = "   ENUM_MEMBER_" + normalizedValue;

        DisplayName = new RichText(myShortName, new TextStyle(FontStyle.Bold));

        if (value.Length <= 32) // protect from too heavy values
        {
          DisplayTypeName = new RichText("= " + value, new TextStyle(FontStyle.Regular, SystemColors.GrayText));
        }
      }

#if RESHARPER92

      public int Identity
      {
        get { return 0; }
      }

      private LookupItemPlacement myPlacement;

      public LookupItemPlacement Placement
      {
        get { return myPlacement ?? (myPlacement = new LookupItemPlacement(myIdentity)); }
        set { myPlacement = value; }
      }

#else

      public string Identity
      {
        get { return myIdentity; }
      }

      private LookupItemPlacement myPlacement;

      public LookupItemPlacement Placement
      {
        get { return myPlacement ?? (myPlacement = new LookupItemPlacement(Identity)); }
        set { myPlacement = value; }
      }

#endif

      public MatchingResult Match(PrefixMatcher prefixMatcher, ITextControl textControl)
      {
        return prefixMatcher.Matcher(myShortName);
      }

      public void Accept(ITextControl textControl, TextRange nameRange, LookupItemInsertType insertType,
                         Suffix suffix, ISolution solution, bool keepCaretStill)
      {
        textControl.Document.ReplaceText(nameRange, "E()");

        var psiServices = solution.GetPsiServices();
        psiServices.Files.CommitAllDocuments();

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
            return memberCheck.CreateTreeElementPointer();
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
      private IReferenceExpression FindReferenceExpression([NotNull] ITextControl textControl, [NotNull] ISolution solution)
      {
        var dotRange = myDotRangeMarker.DocumentRange;
        if (!dotRange.IsValid()) return null;

        var tokenOffset = dotRange.TextRange.StartOffset;
        foreach (var token in TextControlToPsi.GetElements<ITokenNode>(solution, textControl.Document, tokenOffset))
        {
          if (token.GetTokenType() == CSharpTokenType.DOT)
          {
            var expression = token.Parent as IReferenceExpression;
            if (expression != null) return expression;
          }
        }

        return null;
      }

      public IconId Image
      {
        get { return PsiSymbolsThemedIcons.EnumMember.Id; }
      }

      public RichText DisplayName { get; private set; }
      public RichText DisplayTypeName { get; private set; }
    }
  }
}