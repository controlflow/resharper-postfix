ReSharper Postfix Completion plugin
-----------------------------------

The idea is to prevent caret jumps backwards while typing C# code.

[See it in action](http://screencast.com/t/zqMDGTMDqhp)

#### Download

This plugin is available in ReSharper 8.0 Extension Manager gallery.
ReSharper 7.1 version will came shortly.

#### Available templates

* `.arg` helps surround argument with invocation `Method(expr)`
* `.await` awaits expression with C# await keyword `await expr`
* `.cast` surrounds expression with cast `(SomeType) expr`
* `.null` checks nullable expression to be null `if (expr == null)`
* `.notnull` checks expression to be non-null `if (expr != null)`
* `.foreach` iterates over collection `foreach (var x in expr)`
* `.for` surrounds with loop `for (var i=0; i < expr.Length; i++)`
* `.forr` reverses loop `for (var i=expr.Length; i > 0; i--)`
* `.if` checks boolean expression to be true `if (expr)`
* `.ifnot` checks boolean expression to be false `if (!expr)`
* `.not` negates value of inner boolean expression `!expr`
* `.field` intoduces field for expression `_field = expr;`
* `.prop` introduces property for expression `Prop = expr;`
* `.var` initialize new variable with expression `var x = expr;`
* `.new` produces instantiation expression for type `new T()`
* `.paren` surrounds outer expression with parentheses `(expr)`
* `.return` returns value from method/property `return expr;`
* `.yield` yields value from iterator method `yield return expr;`
* `.throw` throws value of Exception type `throw expr;`
* `.using` surrounds dispoable expression `using (var x = expr)`
* `.while` uses expression as loop condition `while (expr)`

TODO:

* `.lock` surrounds expression with statement `lock (expr)`
* make it works on statements?
* `.try` ?

#### Feedback

Feel free to post any issues or feature requests in [JetBrains YouTrack](http://youtrack.jetbrains.com/issues/RSPL) (select *"PostfixCompletion"* subsystem).

Or contact me directly by email: *alexander.shvedov[at]jetbrains.com*