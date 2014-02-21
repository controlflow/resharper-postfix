// ${COMPLETE_ITEM:notnull}

public class Foo
{
  public void Bar(object a, System.Action f)
  {
    var b = a as String();
    b.nn{caret}
    f();
  }
}