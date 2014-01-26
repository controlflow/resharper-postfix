// ${COMPLETE_ITEM:Bar}

using System;

public enum CoolFlags
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