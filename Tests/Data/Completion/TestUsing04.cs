// ${COMPLETE_ITEM:using}

public class Foo
{
  public void Bar()
  {
    using (StartTransaction())
    {
      StartTransaction().{caret}
    }
  }

  public System.IDisposable StartTransaction() { return null; }
}