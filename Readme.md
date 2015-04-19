[ReSharper](http://jetbrains.com/resharper) Postfix Templates plugin
----------------------------------

The basic idea is to prevent caret jumps backwards while typing C# code.
Kind of surround templates on steroids baked with code completion.


![Demo](https://github.com/controlflow/resharper-postfix/blob/master/Content/postfix.gif)

#### Download

* Currently supported ReSharper versions are `8.0`, `8.1`, `8.2`, `9.0` and `9.1`;
* This plugin is available for download in ReSharper [extensions gallery](http://resharper-plugins.jetbrains.com/) ([8.x](http://resharper-plugins.jetbrains.com/packages/ReSharper.Postfix/) and [9.x](http://resharper-plugins.jetbrains.com/packages/ReSharper.Postfix.R90/));
* ReSharper version `7.1` is no longer supported, last build is [available here](https://dl.dropboxusercontent.com/u/2209105/PostfixCompletion/bin.R7/PostfixCompletion.dll);
* Plugin's [changelog is here](Content/Changelog.md).

#### Features

Available templates:

* `.if` – checks boolean expression to be true `if (expr)`
* `.else` – checks boolean expression to be false `if (!expr)`
* `.null` – checks nullable expression to be null `if (expr == null)`
* `.notnull` – checks expression to be non-null `if (expr != null)`
* `.not` – negates value of inner boolean expression `!expr`
* `.foreach` – iterates over collection `foreach (var x in expr)`
* `.for` – surrounds with loop `for (var i = 0; i < expr.Length; i++)`
* `.forr` – reverse loop `for (var i = expr.Length - 1; i >= 0; i--)`
* `.var` – initialize new variable with expression `var x = expr;`
* `.arg` – helps surround argument with invocation `Method(expr)`
* `.to` – assigns expression to some variable `lvalue = expr;`
* `.await` – awaits expression with C# await keyword `await expr`
* `.cast` – surrounds expression with cast `((SomeType) expr)`
* `.field` – intoduces field for expression `_field = expr;`
* `.prop` – introduces property for expression `Prop = expr;`
* `.new` – produces instantiation expression for type `new T()`
* `.paren` – surrounds outer expression with parentheses `(expr)`
* `.parse` – parses string as value of some type `int.Parse(expr)`
* `.return` – returns value from method/property `return expr;`
* `.typeof` – wraps type usage with typeof-expression `typeof(TExpr)`
* `.switch` – produces switch over integral/string type `switch (expr)`
* `.yield` – yields value from iterator method `yield return expr;`
* `.throw` – throws value of Exception type `throw expr;`
* `.using` – surrounds disposable expression `using (var x = expr)`
* `.while` – uses expression as loop condition `while (expr)`
* `.lock` – surrounds expression with statement `lock (expr)`
* `.sel` – selects expression in editor

Also Postfix Templates including two features sharing the same idea:

* **Static members** of first argument type capatible available just like instance members:
![Static members completion](https://github.com/controlflow/resharper-postfix/blob/master/Content/postfix_static.gif)
* **Enum members** are available over values of enumeration types and produce equality/flag checks:
![Static members completion](https://github.com/controlflow/resharper-postfix/blob/master/Content/postfix_enum.gif)

* TODO: .Length <==> .Count feature

#### Notes

* By now it supports only **C# language** (including C# in **Razor markup**)
* Templates can be **expanded by `Tab` key** just like ReSharper live templates
* You can use ReSharper 8 **double completion** feature to list and invoke all the templates are not normally available in current context
* **Options page** allows to enable/disable specific templates and control braces insertion:
![options](https://github.com/controlflow/resharper-postfix/blob/master/Content/options.png)
* You may also try out similar [postfix completion](http://blog.jetbrains.com/webstorm/2014/08/javascript-postfix-completion/) feature in **IntelliJ IDEA** 14

#### Feedback

Feel free to post any issues or feature requests in [YouTrack](http://youtrack.jetbrains.com/issues/RSPL) (use *"PostfixCompletion"* subsystem).

Or contact directly: *alexander.shvedov[at]jetbrains.com*
