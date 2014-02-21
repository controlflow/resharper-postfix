// ${COMPLETE_ITEM:foreach}

public class FooBar
{
  public void Bar(int[] xs, FooBar coolValues)
  {
    foreach (var someName in xs)
    {
      if (someName > 0)
      {
        coolValues.fe{caret}
      }
    }
  }

  public FooBar GetEnumerator() { return this; }
  public FooBar Current { get { return this; } }
  public bool MoveNext() { return false; }
}