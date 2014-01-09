// ${COMPLETE_ITEM:Bar}

using System;

[Flags] public enum CoolFlags
{
  None, Foo, Bar, Boo
}

class Foo
{
  CoolFlags? CoolFlags;

  void Bar()
  {
    CoolFlags.{caret}
    Bar();
  }
}