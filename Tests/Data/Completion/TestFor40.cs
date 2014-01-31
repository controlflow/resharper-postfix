// ${COMPLETE_ITEM:for}
public class Foo {
  public void Bar(int x) {
    x.{caret}
    for (var i = 0; i < x; x++)
  }
}