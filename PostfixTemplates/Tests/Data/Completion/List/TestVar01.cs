namespace NameSpace {
  namespace Inner {
  }
}

public class Foo
{
  public void Bar()
  {
    NameSpace.Inner.{caret};
  }
}