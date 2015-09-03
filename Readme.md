[ReSharper](http://jetbrains.com/resharper) Postfix Templates plugin
----------------------------------

The basic idea is to prevent caret jumps backwards while typing C# code.
Kind of surround templates on steroids baked with code completion.


![Demo](https://github.com/controlflow/resharper-postfix/blob/master/Content/postfix2.gif)

#### Download

* Currently supported ReSharper versions are `9.1` and `9.2`;
* This plugin is available for download in ReSharper [extensions gallery](http://resharper-plugins.jetbrains.com/packages/ReSharper.Postfix.R90/);
* ReSharper `8.x` and `9.0` is no longer supported, check [extension manager](http://resharper-plugins.jetbrains.com/packages/ReSharper.Postfix/) for latest versions;
* ReSharper `7.1` is no longer supported, last build is [available here](https://dl.dropboxusercontent.com/u/2209105/PostfixCompletion/bin.R7/PostfixCompletion.dll);
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
* `.par` – surrounds outer expression with parentheses `(expr)`
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
  ![Static members completion](https://github.com/controlflow/resharper-postfix/blob/master/Content/postfix_static_methods.gif)

* **Enum members** are available over values of enumeration types to produce equality/flag checks:
  ![Enum members completion](https://github.com/controlflow/resharper-postfix/blob/master/Content/postfix_enums.gif)

* **Length/Count** code completion solves one of the most common mistypings when dealing with arrays or collections:
![Length/Count completion](https://github.com/controlflow/resharper-postfix/blob/master/Content/postfix_lengthcount.gif)
* Create **type parameter from usage** helps declaring generic methods in a postfix way:
![Type parameter completion](https://github.com/controlflow/resharper-postfix/blob/master/Content/postfix_generics.gif)

#### Notes

* By now it supports only **C# language** (including C# in **Razor markup**)
* Templates can be **expanded by `Tab` key** just like ReSharper live templates
* You can use ReSharper 8 **double completion** feature to list and invoke all the templates are not normally available in current context
* ReSharper 9.0 code completion filters can filter items introduced by postfix templates
* **Options page** allows to enable/disable specific templates and control braces insertion:
![options](https://github.com/controlflow/resharper-postfix/blob/master/Content/options.png)
* You may also try out similar [postfix completion](http://blog.jetbrains.com/idea/2014/03/postfix-completion/) feature in **IntelliJ IDEA** 14 (later supported in [WebStorm 9](http://blog.jetbrains.com/webstorm/2014/08/javascript-postfix-completion/) and [PHPStorm 9](http://blog.jetbrains.com/phpstorm/2015/05/postfix-code-completion-for-php-in-phpstorm-9-eap/))

#### Feedback

Feel free to post any issues or feature requests in [YouTrack](http://youtrack.jetbrains.com/issues/RSPL) (use *"PostfixCompletion"* subsystem).

Or contact directly: *alexander.shvedov[at]jetbrains.com*
