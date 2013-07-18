using JetBrains.Annotations;
using JetBrains.Application;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.LinqTools;
using JetBrains.ReSharper.Feature.Services.Lookup;
using JetBrains.ReSharper.Feature.Services.Resources;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.ExtensionsAPI;
using JetBrains.ReSharper.Psi.Pointers;
using JetBrains.ReSharper.Psi.Services;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.TextControl;
using JetBrains.UI.Icons;
using JetBrains.UI.RichText;
using JetBrains.Util;

namespace JetBrains.ReSharper.ControlFlow.PostfixCompletion.LookupItems
{
  public class PostfixLookupItem2 : ILookupItem
  {
    [NotNull] private readonly string myShortcut;
    [NotNull] private readonly ITreeNodePointer<ICSharpExpression> myExpression;
    [NotNull] private readonly ITreeNodePointer<IReferenceExpression> myReplaceExpression;
    private TextRange myReplaceRange;

    public PostfixLookupItem2(
      [NotNull] PostfixTemplateAcceptanceContext context,
      [NotNull] PrefixExpression prefixExpression,
      [NotNull] string shortcut)
    {
      myShortcut = shortcut;
      myExpression = prefixExpression.Expression.CreateTreeElementPointer();
      myReplaceExpression = context.ReferenceExpression.CreateTreeElementPointer();
      myReplaceRange = context.ReplaceRange; // note: minimum replace range
    }

    public MatchingResult Match(string prefix, ITextControl textControl)
    {
      // todo: match "nn" with "notnull"
      return LookupUtil.MatchesPrefixSimple(myShortcut, prefix);
    }

    public void Accept(
      ITextControl textControl, TextRange nameRange, LookupItemInsertType lookupItemInsertType,
      Suffix suffix, ISolution solution, bool keepCaretStill)
    {
      var referenceExpression = myReplaceExpression.GetTreeNode();
      if (referenceExpression == null) return;

      var expression = myExpression.GetTreeNode();
      if (expression == null) return;

      

      // will be removed from tree

      var psiServices = expression.GetPsiServices();
      var psiModule = expression.GetPsiModule();

      var replaceRange = myReplaceRange.Intersects(nameRange)
        ? new TextRange(myReplaceRange.StartOffset, nameRange.EndOffset)
        : myReplaceRange;

      var expressionRange = expression.GetDocumentRange().TextRange;
      replaceRange = expressionRange.JoinRight(replaceRange); // mmm

      if (expression.Contains(referenceExpression))
      {
        var mm = new TreeNodeMarker<IReferenceExpression>(referenceExpression);
        var copy = expression.Copy(expression);
        var rr = mm.GetAndDispose(copy);

        var toFix = rr.QualifierExpression;
        LowLevelModificationUtil.ReplaceChildRange(rr, rr, toFix);
        if (toFix.NextSibling is IErrorElement)
          LowLevelModificationUtil.DeleteChild(toFix.NextSibling);

        expression = copy;
      }
      else
      {
        expression = expression.Copy(expression);
      }

      // todo: replace with 'POSTFIX' for expression-based providers
      textControl.Document.ReplaceText(replaceRange, "POSTFIX;");
      solution.GetPsiServices().CommitAllDocuments();

      int? caretPos = null;

      using (WriteLockCookie.Create())
      psiServices.Transactions.Execute("AAAA", () =>
      {
        var re = TextControlToPsi.GetElements<IExpressionStatement>(
          solution, textControl.Document, replaceRange.StartOffset);

        foreach (var statement in re)
        {
          if (IsMarkerExpressionStatement(statement, "POSTFIX"))
          {
            var factory = CSharpElementFactory.GetInstance(psiModule);
            var ifStatement = (IIfStatement)factory.CreateStatement("if (expr) CARET;");

            var c = new RecursiveElementCollector<IExpressionStatement>(es => IsMarkerExpressionStatement(es, "CARET"));
            var cm = new TreeNodeMarker<IExpressionStatement>();

            var caretNode = c.ProcessElement(ifStatement).GetResults();
            if (caretNode.Count == 1)
            {
              cm.Mark(caretNode[0]);
            }

            //if (expression.Contains(referenceExpression))
            {
              expression.SetResolveContextForSandBox(statement, SandBoxContextType.Child);

              
            }

            ifStatement = statement.ReplaceBy(ifStatement);
            ifStatement.Condition.ReplaceBy(expression);

            var cnode = cm.FindMarkedNode(ifStatement);
            if (cnode != null)
            {
              var pos = cnode.GetDocumentRange().TextRange.StartOffset;
              LowLevelModificationUtil.DeleteChild(cnode);
              caretPos = pos;
              
            }

            cm.Dispose(ifStatement);
          }
        }

        //referenceExpression.ReplaceBy(expression);

        
      });


      AfterComplete(textControl, suffix, caretPos);
    }

    protected virtual void AfterComplete(
      [NotNull] ITextControl textControl, [NotNull] Suffix suffix, int? caretPosition)
    {
      if (caretPosition != null)
        textControl.Caret.MoveTo(caretPosition.Value, CaretVisualPlacement.DontScrollIfVisible);

      suffix.Playback(textControl);
    }

    private static bool IsMarkerExpressionStatement(
      [NotNull] IExpressionStatement expressionStatement, [NotNull] string markerName)
    {
      var reference = expressionStatement.Expression as IReferenceExpression;
      return reference != null
          && reference.QualifierExpression == null
          && reference.Delimiter == null
          && reference.NameIdentifier.Name == markerName;
    }

    public IconId Image
    {
      get { return ServicesThemedIcons.LiveTemplate.Id; }
    }

    public RichText DisplayName { get { return myShortcut; } }
    public RichText DisplayTypeName { get { return null; } }

    public TextRange GetVisualReplaceRange(ITextControl textControl, TextRange nameRange)
    {
      // note: prefix highlighter disallows highlighting to be any position
      return TextRange.InvalidRange;
    }

    public bool AcceptIfOnlyMatched(LookupItemAcceptanceContext itemAcceptanceContext)
    {
      return false;
    }

    public bool CanShrink { get { return false; } }
    public bool Shrink() { return false; }
    public void Unshrink() { }

    public string OrderingString { get { return myShortcut; } }
    public string Identity { get { return myShortcut; } }

#if RESHARPER8
    public int Multiplier { get; set; }
    public bool IsDynamic { get { return false; } }
    public bool IgnoreSoftOnSpace { get; set; }
#endif
  }

  //public class PostfixStatementLookupItem : PostfixLookupItem2
  //{
  //  public PostfixStatementLookupItem(
  //    [NotNull] PostfixTemplateAcceptanceContext context, [NotNull] string shortcut)
  //    : base(context, shortcut) { }
  //
  //
  //}
}