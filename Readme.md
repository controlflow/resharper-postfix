ReSharper Postfix Completion plugin
-----------------------------------

The idea is to prevent caret jumps backwards while typing C# code, [see it in action](http://screencast.com/t/zqMDGTMDqhp).

#### Download

This plugin is available in ReSharper 8.0 Extension Manager [gallery](http://resharper-plugins.jetbrains.com/packages/ReSharper.Postfix/).

ReSharper 7.1 version is available for [download here](https://dl.dropboxusercontent.com/u/2209105/PostfixCompletion/bin.R7/PostfixCompletion.dll).

#### Features

Currently available templates:

* `.arg` – helps surround argument with invocation `Method(expr)`
* `.await` – awaits expression with C# await keyword `await expr`
* `.cast` – surrounds expression with cast `(SomeType) expr`
* `.null` – checks nullable expression to be null `if (expr == null)`
* `.notnull` – checks expression to be non-null `if (expr != null)`
* `.foreach` – iterates over collection `foreach (var x in expr)`
* `.for` – surrounds with loop `for (var i = 0; i < expr.Length; i++)`
* `.forr` – reverse loop `for (var i = expr.Length; i >= 0; i--)`
* `.if` – checks boolean expression to be true `if (expr)`
* `.ifnot` – checks boolean expression to be false `if (!expr)`
* `.not` – negates value of inner boolean expression `!expr`
* `.field` – intoduces field for expression `_field = expr;`
* `.prop` – introduces property for expression `Prop = expr;`
* `.var` – initialize new variable with expression `var x = expr;`
* `.new` – produces instantiation expression for type `new T()`
* `.paren` – surrounds outer expression with parentheses `(expr)`
* `.return` – returns value from method/property `return expr;`
* `.yield` – yields value from iterator method `yield return expr;`
* `.throw` – throws value of Exception type `throw expr;`
* `.using` – surrounds disposable expression `using (var x = expr)`
* `.while` – uses expression as loop condition `while (expr)`
* `.lock` – surrounds expression with statement `lock (expr)`

Templates availability depend on context where code completion is executed - for example, `.notnull` template
will not be available if some expression is already known to be not-null value in some particular context,
`.using` template will be available only on expression of `IDisposable` type and so on.

But any time you can invoke code completion one more time ("double completion" feature of ReSharper 8) and
it will shows up with all the postfix templates available to write, without any semantic checks.

#### Feedback

Feel free to post any issues or feature requests in [YouTrack](http://youtrack.jetbrains.com/issues/RSPL) (use *"PostfixCompletion"* subsystem).

Or contact directly: *alexander.shvedov[at]jetbrains.com*