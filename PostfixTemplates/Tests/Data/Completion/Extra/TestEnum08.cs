// ${COMPLETE_ITEM:Bar}

class Generic<T, U>
{
  public enum CoolFlags
  {
    None,
    Foo,
    Bar
  }
}

class Foo<T>
{
  public Generic<int, T>.CoolFlags MyFlags;
  public bool M(Foo<string> foo) => foo?.MyFlags.{caret}
}