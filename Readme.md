Simple ReSharper code completion/templating plugin.

The idea is to prevent caret jumps backwards while typing C# code.

## Download

| Version |   |   |
| ------- | - | - |
| ReSharper 7.0 | [debug](https://dl.dropbox.com/u/2209105/PostfixCompletion/bin/Debug/PostfixCompletion.dll)    | [release](https://dl.dropbox.com/u/2209105/PostfixCompletion/bin/Release/PostfixCompletion.dll) |
| ReSharper 8.0 | [debug](https://dl.dropbox.com/u/2209105/PostfixCompletion/bin.R8/Debug/PostfixCompletion.dll) | [release](https://dl.dropbox.com/u/2209105/PostfixCompletion/bin.R8/Release/PostfixCompletion.dll) |

## Available templates

Iterting over all kinds of collections, reverse iteration:

![foreach](/img/foreach.png)

Wrapping up boolean expressions with *if-statement*:

![if/ifnot](/img/if.png)

Checking nullable expressions for *null*:

![null/notnull](/img/notnull.png)

Surrounding *IDisposable* expressions with *using-statemenet*:

![using](/img/using.png)

Awaiting expressions of *'Task<T>'* type:

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