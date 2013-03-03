Simple ReSharper code completion/templating plugin.
The idea is to prevent caret jumps backwards when typing C# code.

## Available templates

Iterting over all kinds of collections, reverese iteration:
![foreach](/img/foreach.png)
Wrapping up boolean expressions with *if-statement*:
![if/ifnot](/img/if.png)
Checking nullable expressions for *null*:
![null/notnull](/img/notnull.png)
Surrounding *IDisposable* expressions with *using-statemenet*:
![using](/img/using.png)
Awaiting expressions of 'Task<T>' type:
![await](/img/await.png)

.var

.field

.not

.return

.throw

.while/.whilenot

## Future work

.check
.yield
.field
.switch
.format

collections:
.ifempty/empty
.ifsingle/single
.ifnonempty

types:
int.list => List<int>?
Foo.new => new Foo()


## Feedback

alexander.shvedov[at]jetbrain.com