// ${COMPLETE_ITEM:not}

public class Foo
{
  public void Bar(bool b, int i)
  {
    b &&    i > 10 
           .{caret}
  }
}