// ${COMPLETE_ITEM:var}

class Foo {
  void Bar(string a) {
     Bar(a + Foo.{caret});
  }
}