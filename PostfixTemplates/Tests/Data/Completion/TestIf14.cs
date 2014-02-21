// ${COMPLETE_ITEM:if}

public class Foo
{
  public void Bar(bool b)
  {
    F().{caret}
  }
  
  public bool F() { return true; }
}