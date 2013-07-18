using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.ReSharper.ControlFlow.PostfixCompletion.LookupItems;
using JetBrains.ReSharper.Feature.Services.Lookup;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.Util;

namespace JetBrains.ReSharper.ControlFlow.PostfixCompletion.TemplateProviders
{
  [PostfixTemplateProvider("field", "Introduces field for expression in constructors")]
  public sealed class IntroduceFieldTemplateProvider : IPostfixTemplateProvider
  {
    public void CreateItems(PostfixTemplateAcceptanceContext context, ICollection<ILookupItem> consumer)
    {
      var declaration = context.ContainingFunction;
      if (declaration == null) return;

      if (context.ForceMode || declaration.DeclaredElement is IConstructor)
      {
        foreach (var expression in context.PossibleExpressions)
        {
          if (expression.ExpressionType.IsUnknown) continue;
          if (!expression.CanBeStatement) continue;

          var referenceExpression = expression.Expression as IReferenceExpression;
          if (referenceExpression != null && referenceExpression.QualifierExpression == null)
          {
            // filter out other fields
            var target = expression.ReferencedElement;
            if (target == null || target is IField || target is IProperty) continue;
          }

          consumer.Add(new LookupItem(expression, declaration.IsStatic, expression.ExpressionType));
          break;
        }
      }
    }

    private sealed class LookupItem : StatementPostfixLookupItem<IExpressionStatement>
    {
      public LookupItem([NotNull] PrefixExpressionContext expression, bool isStatic, IType expressionType)
        : base("field", expression)
      {
        IsStatic = isStatic;
        ExpressionType = expressionType;
      }

      public bool IsStatic { get; private set; }
      public IType ExpressionType { get; private set; }

      protected override IExpressionStatement CreateStatement(IPsiModule psiModule, CSharpElementFactory factory)
      {
        return (IExpressionStatement) factory.CreateStatement("fieldName = expression;");
      }

      protected override void PutExpression(IExpressionStatement statement, ICSharpExpression expression)
      {
        var typeDecl = statement.GetContainingNode<IClassDeclaration>().NotNull();
        var lastField = typeDecl.MemberDeclarations.LastOrDefault(x =>
          x.DeclaredElement is IField || x.IsStatic == IsStatic);

        var factory = CSharpElementFactory.GetInstance(typeDecl);
        var field = factory.CreateFieldDeclaration(ExpressionType, "boo");
        var foo = typeDecl.AddClassMemberDeclarationAfter(field, (IClassMemberDeclaration) lastField);

        var a = (IAssignmentExpression) statement.Expression;
        a.SetSource(expression);
        ((IReferenceExpression) a.Dest).Reference.BindTo(foo.DeclaredElement);

      }

      //protected override void AfterComplete(
      //  ITextControl textControl, Suffix suffix, ICSharpExpression expression, int? caretPosition)
      //{
      //  // note: yes, we are supressing suffix, since there is no nice way to preserve it
      //
      //  if (expression == null) return;
      //
      //  const string name = "IntroFieldAction";
      //  var solution = expression.GetSolution();
      //  var rules = DataRules
      //    .AddRule(name, ProjectModel.DataContext.DataConstants.SOLUTION, solution)
      //    .AddRule(name, DataConstants.DOCUMENT, textControl.Document)
      //    .AddRule(name, TextControl.DataContext.DataConstants.TEXT_CONTROL, textControl)
      //    .AddRule(name, Psi.Services.DataConstants.SELECTED_EXPRESSION, expression);
      //
      //  Lifetimes.Using(lifetime =>
      //    WorkflowExecuter.ExecuteBatch(
      //      Shell.Instance.GetComponent<DataContexts>().CreateWithDataRules(lifetime, rules),
      //      new IntroFieldWorkflow(solution, null)));
      //
      //  // todo: rename hotspots
      //
      //  var ranges = textControl.Selection.Ranges.Value;
      //  if (ranges.Count == 1) // reset selection
      //  {
      //    var endPos = ranges[0].End;
      //    textControl.Selection.SetRanges(new[] { new TextControlPosRange(endPos, endPos) });
      //  }
      //}
    }
  }
}