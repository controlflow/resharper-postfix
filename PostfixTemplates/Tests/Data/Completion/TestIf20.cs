// ${COMPLETE_ITEM:if}

class Program {
  void F(int[] xs) {
    xs.Length > 0.{caret}
    (xs != null ? xs : xs)[42] = 42;
  }
}