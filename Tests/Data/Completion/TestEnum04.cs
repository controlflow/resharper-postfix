// ${COMPLETE_ITEM:Bar}

using System;

[Flags] public enum CoolFlags
{
  None, Foo, Bar, Boo
}

class Foo
{
  CoolFlags? myFoo;

  void Bar()
  {
    myFoo.{caret}
    Bar();
  }
}