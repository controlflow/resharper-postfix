// ${COMPLETE_ITEM:using}

public class Foo
{
  public void Bar()
  {
    StartTransaction().u{caret}
  }

  public System.IDisposable StartTransaction() { return null; }
}