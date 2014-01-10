class Program
{
  static Program Main(object a)
  {
    a as Program.{caret}
  }

  public static implicit operator bool(Program p)
  {
    return true;
  }
}