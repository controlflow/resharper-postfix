using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.Application.Settings;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.CodeCompletion;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.AspectLookupItems.BaseInfrastructure;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.AspectLookupItems.Matchers;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.AspectLookupItems.Presentations;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.LookupItems;
using JetBrains.ReSharper.Feature.Services.CSharp.CodeCompletion.Infrastructure;
using JetBrains.ReSharper.Feature.Services.Lookup;
using JetBrains.ReSharper.Feature.Services.Util;
using JetBrains.ReSharper.Features.Intellisense.CodeCompletion.CSharp.AspectLookupItems;
using JetBrains.ReSharper.Features.Intellisense.CodeCompletion.CSharp.Rules;
using JetBrains.ReSharper.PostfixTemplates.Settings;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Impl.Resolve;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Resolve.Managed;
using JetBrains.ReSharper.Psi.Pointers;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.ReSharper.Resources.Shell;
using JetBrains.Text;
using JetBrains.TextControl;
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
      if (referenceExpression.HasConditionalAccessSign) return false;

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

      return AddEnumerationMembers(context, qualifierType, collector);
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

    private static readonly TextStyle EnumValueStyle = new TextStyle(FontStyle.Regular, SystemColors.GrayText);

    private static bool AddEnumerationMembers(
      [NotNull] CSharpCodeCompletionContext context, [NotNull] IDeclaredType enumerationType, [NotNull] GroupedItemsCollector collector)
    {
      var enumTypeElement = enumerationType.GetTypeElement() as IEnum;
      if (enumTypeElement == null) return false;

      var enumSubstitution = enumerationType.GetSubstitution();
      var enumMembersWithValues = new List<DeclaredElementInstance, string>();

      var isFlagsEnum = enumTypeElement.HasAttributeInstance(PredefinedType.FLAGS_ATTRIBUTE_CLASS, false);

      foreach (var enumMember in enumTypeElement.EnumMembers)
      {
        var enumValue = enumMember.ConstantValue.Value;
        var enumCase = new DeclaredElementInstance(enumMember, enumSubstitution);

        if (isFlagsEnum)
        {
          var convertible = enumValue as IConvertible;
          if (convertible != null) enumMembersWithValues.Add(enumCase, GetBinaryRepresentation(convertible));
        }
        else
        {
          var formattable = enumValue as IFormattable;
          if (formattable != null) enumMembersWithValues.Add(enumCase, formattable.ToString("D", CultureInfo.InvariantCulture));
        }
      }

      if (enumMembersWithValues.Count == 0) return false;

      var maxLength = enumMembersWithValues.Max(x => x.Second.Length);
      var index = 0;

      foreach (var enumMember in enumMembersWithValues)
      {
        var elementInstance = enumMember.First;
        var elementValue = isFlagsEnum ? enumMember.Second.PadLeft(maxLength, '0') : enumMember.Second;

        var info = new EnumCaseInfo(elementInstance, context, isFlagsEnum, elementValue);
        info.Placement.OrderString = "___ENUM_MEMBER_" + index.ToString("D16");
        info.Ranges = context.CompletionRanges;

        index++;

        var lookupItem = LookupItemFactory.CreateLookupItem(info)
          .WithPresentation(item =>
          {
            var presentation = new DeclaredElementPresentation<CSharpDeclaredElementInfo>(item.Info, PresenterStyles.DefaultPresenterStyle);

            var caseValue = item.Info.CaseValue;
            if (caseValue.Length <= 32)
              presentation.DisplayTypeName = new RichText("= " + caseValue, EnumValueStyle);

            return presentation;
          })
          .WithMatcher(item => new DeclaredElementMatcher(item.Info, IdentifierMatchingStyle.Default))
          .WithBehavior(item => new EnumCaseCheckBehavior(item.Info));

        collector.Add(lookupItem);
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

    private sealed class EnumCaseInfo : CSharpDeclaredElementInfo
    {
      public EnumCaseInfo([NotNull] DeclaredElementInstance instance, [NotNull] CSharpCodeCompletionContext context, bool isFlagsEnum, [NotNull] string caseValue)
        : base(instance.Element.ShortName, instance, context.BasicContext.LookupItemsOwner, context, context.BasicContext)
      {
        IsFlagsEnum = isFlagsEnum;
        CaseValue = caseValue;
      }

      public bool IsFlagsEnum { get; private set; }
      [NotNull] public string CaseValue { get; private set; }

      public bool IsZeroCase { get { return CaseValue.IndexOf('1') == -1; } }

      public bool IsMultiBitFlagCase
      {
        get { return CaseValue.IndexOf('1') != CaseValue.LastIndexOf('1'); }
      }
    }

    private sealed class EnumCaseCheckBehavior : LookupItemAspect<EnumCaseInfo>, ILookupItemBehavior
    {
      public EnumCaseCheckBehavior([NotNull] EnumCaseInfo info) : base(info) { }

      public bool AcceptIfOnlyMatched(LookupItemAcceptanceContext itemAcceptanceContext) { return false; }

      private const string CASE_COMPLETION_NAME = "___ENUM_CASE_COMPLETION";

      public void Accept(
        ITextControl textControl, TextRange nameRange, LookupItemInsertType lookupItemInsertType,
        Suffix suffix, ISolution solution, bool keepCaretStill)
      {
        textControl.Document.ReplaceText(nameRange, CASE_COMPLETION_NAME + "()");

        var psiServices = solution.GetPsiServices();
        psiServices.Files.CommitAllDocuments();

        var enumMember = Info.PreferredDeclaredElement;
        if (enumMember == null) return;

        var invocationExpression = FindFakeInvocation(textControl, solution, nameRange.EndOffset);
        if (invocationExpression == null) return;

        var factory = CSharpElementFactory.GetInstance(invocationExpression);
        var template = (Info.IsFlagsEnum && !Info.IsZeroCase)
          ? (Info.IsMultiBitFlagCase ? "($0 & $1) != $1" : "($0 & $1) != 0")
          : "$0 == $1";

        var referenceExpression = (IReferenceExpression) invocationExpression.InvokedExpression;
        var qualifierExpression = referenceExpression.QualifierExpression;

        var enumMemberCheck = factory.CreateExpression(template, qualifierExpression, enumMember);

        var caretPointer = psiServices.DoTransaction(
          commandName: typeof(EnumCaseCheckBehavior).FullName,
          func: () =>
          {
            using (WriteLockCookie.Create())
            {
              var memberCheck = invocationExpression.ReplaceBy(enumMemberCheck);
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
      private static IInvocationExpression FindFakeInvocation([NotNull] ITextControl textControl, [NotNull] ISolution solution, int offset)
      {
        foreach (var invocationExpression in TextControlToPsi.GetElements<IInvocationExpression>(solution, textControl.Document, offset))
        {
          var referenceExpression = invocationExpression.InvokedExpression as IReferenceExpression;
          if (referenceExpression == null) continue;

          var nameIdentifier = referenceExpression.NameIdentifier;
          if (nameIdentifier == null) continue;

          if (nameIdentifier.Name == CASE_COMPLETION_NAME) return invocationExpression;
        }

        return null;
      }
    }
  }
}