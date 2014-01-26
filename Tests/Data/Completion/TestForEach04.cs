// ${COMPLETE_ITEM:foreach}
using System.Collections.Generic;

public class FooBar
{
  public void Bar(Dictionary<int, int> xs)
  {
    xs.{caret}
    foreach (var keyValuePair in xs)
    {
      
    }
  }
}