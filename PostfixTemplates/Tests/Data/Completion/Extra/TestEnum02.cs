// ${COMPLETE_ITEM:None}

using System;

class A<T>
{
  [Flags]
  public enum CoolFlags : ulong
  {
    None = 0,

    Foo = 1 << 0,
    Bar = 1 << 1,
    Boo = 1 << 2,

    All = Foo | Bar | Boo
  }
}

class ReallyCoolObject<T>
{
  public A<T>.CoolFlags SomeProperty { get; set; }
}

class Foo
{
  void Bar(ReallyCoolObject<int> obj)
  {
    obj.SomeProperty.{caret}
  }
}