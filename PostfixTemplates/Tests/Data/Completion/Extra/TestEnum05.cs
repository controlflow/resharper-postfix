// ${COMPLETE_ITEM:None}

[System.Flags]
public enum CoolFlags
{
  None,
  Foo = 1 << 1,
  Bar = 1 << 2,
  Boo = Foo | Bar
}

class Foo
{
  bool M(CoolFlags cf) => cf.{caret}
}