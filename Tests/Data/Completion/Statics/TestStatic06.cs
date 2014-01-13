// ${COMPLETE_ITEM:Synchronized}

class Foo {
  public void M(System.Action f, System.IO.TextWriter writer, int[] xs) {
    f(() => {
      foreach (var x in xs) {
        var smth = x + 1;
        writer.syn{caret}
      }
    });
  }
}