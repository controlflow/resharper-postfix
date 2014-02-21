class Program
{
  static Program Main(object a)
  {
    var program = a as Program;
    program.{caret}
  }

  public static implicit operator bool(Program p)
  {
    return true;
  }
}