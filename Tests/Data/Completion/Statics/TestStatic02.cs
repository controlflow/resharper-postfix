// ${COMPLETE_ITEM:TwoFooArg}

class Base
{
  public static void SingleFooArg(A foo) { }
  public static void BaseMethod(A foo) { }
}

class A : Base
{
  public static void NoArgs() { }
  public static void SingleIntArg(int a) { }
  public new static void SingleFooArg(A foo) { }
  public static void FooArgOverloaded(A foo) { }
  public static void FooArgOverloaded(A foo, A bar) { }
  public static void FooArgOverloaded(A foo, A bar, int i) { }
  public static void TwoFooArg(A foo, A bar) { }
}

class Foo
{
  void Bar(A foo)
  {
    foo.{caret}
  }
}