// ${COMPLETION_TYPE:Insert}
// ${COMPLETE_ITEM:ToArray}
// ${PARENS:Left}

class Foo
{
  void M(Bar bar)
  {
    bar.toa{caret}
  }
}

class Bar
{
  public static int[] ToArray(Bar bar)
  {
    return null;
  }
}