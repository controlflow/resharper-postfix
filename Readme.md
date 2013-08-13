ReSharper Postfix Completion plugin
-----------------------------------

The idea is to prevent caret jumps backwards while typing C# code.

[See it in action](http://screencast.com/t/zqMDGTMDqhp)

#### Download

Pre-release package version is available in in ReSharper 8.0 Extension Manager gallery.
ReSharper 7.1 version will came shortly.

#### Available templates

Iterting over all kinds of collections, reverse iteration:

![foreach](/Content/foreach.png)

* `.arg` helps surround argument with invocation `Method(expr)`
* `.await` awaits expression with C# await keyword `await expr`
* `.cast` surrounds expression with cast `(SomeType) expr`
* `.null` checks nullable expression to be null `if (expr == null)`
* `.notnull` checks expression to be non-null `if (expr != null)`
* `.foreach` iterates over collection `foreach (var x in expr)`
* `.for` surrounds with loop `for (var i=0; i < expr.Length; i++)`
* .forr
* .if
* .ifnot
* .not
* .field
* .prop
* .var
* .not
* .new
* .paren
* .return
* .throw
* .using
* .while

TODO:

* .lock
* work on statements?
** .try
** .if


// TODO: other templates

#### Feedback

Feel free to post any issues or feature requests in [JetBrains YouTrack](http://youtrack.jetbrains.com/issues/RSPL) (select *"PostfixCompletion"* subsystem).

Or contact me directly by email: *alexander.shvedov[at]jetbrains.com*