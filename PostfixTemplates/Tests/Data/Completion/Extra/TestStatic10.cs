// ${COMPLETE_ITEM:GetValues}

class Foo {
  static void Main(string[] args) {
    SomeEnum.{caret}
  }
}

enum SomeEnum {
  CaseA,
  CaseB,
  OtherCase
}