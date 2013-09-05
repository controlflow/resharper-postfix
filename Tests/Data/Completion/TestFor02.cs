// ${COMPLETE_ITEM:for}

class Foo
{
  void Bar()
  {
    var xs = new Foo();
    xs.for{caret}
  }

  public int Count { get; set; }
}