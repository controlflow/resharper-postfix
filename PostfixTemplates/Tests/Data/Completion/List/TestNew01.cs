enum E { A }

class Foo {
  void Bar() {
    E.{caret}
    (E.A).ToString();
  }
}