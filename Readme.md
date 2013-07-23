Simple ReSharper code completion/templating plugin.

The idea is to prevent caret jumps backwards while typing C# code.

[See it in action](http://screencast.com/t/zqMDGTMDqhp)

#### Download

Will be available in ReSharper 8.0 extension gallery soon.
ReSharper 7.1 version will came shortly after.

#### Available templates

Iterting over all kinds of collections, reverse iteration:

![foreach](/Content/foreach.png)

Wrapping up boolean expressions with *if-statement*:

![if/ifnot](/Content/if.png)

Checking nullable expressions for *null*:

![null/notnull](/Content/notnull.png)

Surrounding *IDisposable* expressions with *using-statemenet*:

![using](/Content/using.png)

Awaiting expressions of *'Task<T>'* type:

![await](/Content/await.png)

* .var
* .field
* .not
* .return
* .throw
* .while/.whilenot

## Future work

* .check
* .yield
* .field
* .switch
* .format
collections:
* .ifempty/empty
* .ifsingle/single
* .ifnonempty

types:
* int.list => List<int>?
* Foo.new => new Foo()

#### Feedback

Feel free to create issues in [JetBrains YouTrack](http://youtrack.jetbrains.com/issues/RSPL) for "PostfixCompletion" subsystem.
Or contact me directly by email: *alexander.shvedov[at]jetbrains.com*