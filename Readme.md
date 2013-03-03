Simple ReSharper code completion/templating plugin.
The idea is to prevent caret jumps backwards when typing C# code.

Currently available templates:

![foreach](/img/foreach.png)

![if/ifnot](/img/if.png)

![null/notnull](/img/notnull.png)

![using](/img/using.png)

![await](/img/await.png)

.var

.field

.not

.return

.throw

.while/.whilenot

Future work:

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


Feedback:

alexander.shvedov[at]jetbrain.com