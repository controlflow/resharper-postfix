// ${COMPLETE_ITEM:using}

public class Foo : System.IDisposable
{
  public void Bar()
  {
    var foo = new Foo();
    foo.u{caret}
  }
}