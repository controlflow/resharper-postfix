class Program<T>
{
  static Program<T> Main(object a)
  {
    a as Program<T>.{caret}
  }

  public static implicit operator bool(Program p)
  {
    return true;
  }
}