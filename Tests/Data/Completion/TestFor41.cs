// ${COMPLETE_ITEM:forr}
public class Foo {
  public void Bar(short x) {
    x.{caret}
    for (var i = x; i > x; i--)
  }
}