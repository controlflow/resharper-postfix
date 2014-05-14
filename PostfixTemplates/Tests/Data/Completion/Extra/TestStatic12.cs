// ${COMPLETE_ITEM:M}

public class C {
  void Main() {
    this.m{caret}(12);
  }

  static void M(C c) { }
  static void M(C c, int x, int y) { }
  static void M(C c, string x) { }
}
