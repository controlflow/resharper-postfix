// ${COMPLETE_ITEM:foreach}

public class Foo
{
  public void Bar(int[] xs, int[] ys)
  {
    foreach (var someName in xs)
    {
      if (someName > 0)
      {
        ys.fe{caret}
      }
    }
  }
}