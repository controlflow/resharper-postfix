// ${COMPLETE_ITEM:if}

class Program {
  void F(object element) {
    var str = element as string;
    if (str != null) {
      element = str.Length > 0 ? (object) str.Length : null;
      element == null.{caret};

      var boo = 123;
    }
  }
}