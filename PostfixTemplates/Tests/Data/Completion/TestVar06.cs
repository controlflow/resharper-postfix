// ${COMPLETE_ITEM:var}

class Foo
{
  Foo(string s) { }
  void Bar()
  {
     Foo.var{caret}
  }
}