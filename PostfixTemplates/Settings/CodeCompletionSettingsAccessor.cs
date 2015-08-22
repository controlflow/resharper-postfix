using System;
using System.Linq.Expressions;
using JetBrains.ReSharper.Feature.Services.CodeCompletion;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Settings;

namespace JetBrains.ReSharper.PostfixTemplates.Settings
{
  // todo: [R#] check we can get rid of this in R# source code
  public static class CodeCompletionSettingsAccessor
  {
    public readonly static Expression<Func<CodeCompletionSettingsKey, ParenthesesInsertType>>
      ParenthesesInsertType = x => x.ParenthesesInsertType;
  }
}