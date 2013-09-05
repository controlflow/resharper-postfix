// ${COMPLETE_ITEM:throw}

class Foo
{
  void Bar()
  {
    E<int>.throw{caret}

    var t = 123;
  }

  class E<T> : System.Exception
  {
    public E(string name) { }
  }
}