// ${COMPLETE_ITEM:lock}

class Foo
{
  private readonly object _syncLock = new object();

  void Bar()
  {
    _syncLock.{caret}

    var a = 123;
  }
}