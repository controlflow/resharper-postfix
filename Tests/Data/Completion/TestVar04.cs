// ${COMPLETE_ITEM:var}

class Foo
{
  void Bar(object x)
  {
    int referenceName;

    Boo1() && Boo2().{caret}
    referenceName = Blah();
  }

  bool Boo1() { return true; }
  bool Boo2() { return true; }
  int Blah() { return 123; }
}