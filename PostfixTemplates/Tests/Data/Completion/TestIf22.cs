// ${COMPLETE_ITEM:if}

class Foo {
  void Bar() {
    "aaa".IsNullOrEmpty().i{caret}
  }
}

static class StringExtensions {
  public static bool IsNullOrEmpty(this string s) {
    return string.IsNullOrEmpty(s);
  }
}