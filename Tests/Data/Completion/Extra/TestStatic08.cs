// ${COMPLETE_ITEM:Bar}

class Foo {
  static void Bar<T>(ref T a) { }
  static void Main(string[] args) {
    var foo = new Foo();
    foo.Bar{caret}
  }
}