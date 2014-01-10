// ${COMPLETE_ITEM:foreach}

public class Foo
{
  public void Bar(int[] xs, IEnumerable coolValues)
  {
    foreach (var someName in xs)
    {
      if (someName > 0)
      {
        coolValues.fe{caret}
      }
    }
  }
}