// ${COMPLETE_ITEM:M2}

class Foo : GenericBase<int>
{
  void Bar(Foo foo)
  {
    foo.m2{caret}
  }
}

class GenericBase<T>
{
  public static void M2(GenericBase<T> foo, T x) { }
}