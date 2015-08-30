// ${COMPLETE_ITEM:foreach}
using System.Collections.Generic;

class Foo
{
  private List<int> myList1;
  private List<int> myList2;

  void M()
  {
    myList1 = new List<int>();

    myList1.f{caret}

    myList2 = new List<int>();
  }
}