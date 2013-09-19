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
* `.parse` – parses string as value of some type `int.Parse(expr)`
* `.return` – returns value from method/property `return expr;`
* `.switch` – produces switch over integral/string type `switch (expr)`
* `.yield` – yields value from iterator method `yield return expr;`
* `.throw` – throws value of Exception type `throw expr;`
* `.using` – surrounds disposable expression `using (var x = expr)`
* `.while` – uses expression as loop condition `while (expr)`
* `.lock` – surrounds expression with statement `lock (expr)`

Template availability depends on context where code completion is executed - for example, `.notnull` template
is not be available if some expression is known to be not-null value in some particular context,
`.using` template will be available only on expression of `IDisposable` type and so on.

You can invoke code completion one more time (*"double completion"* feature of ReSharper 8) and
it will came up with all the postfix templates available, without any semantic filtering.

TODO: static members helpers

TODO: enum helpers

Options page allows to enable/disable specific templates and control braces inserion:
![options](/Content/options.png)

#### Feedback

Feel free to post any issues or feature requests in [YouTrack](http://youtrack.jetbrains.com/issues/RSPL) (use *"PostfixCompletion"* subsystem).

Or contact directly: *alexander.shvedov[at]jetbrains.com*