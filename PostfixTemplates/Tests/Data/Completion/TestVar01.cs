// ${COMPLETE_ITEM:var}

class Foo
{
  private string myField;
  string Bar(object x)
  {
    System.Console.ReadLine().{caret}
    myField = System.Console.ReadLine();
  }
}