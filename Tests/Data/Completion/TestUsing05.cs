// ${COMPLETE_ITEM:using}

public class CoolType : System.IDisposable
{
  public CoolType GetSomething()
  {
    GetSomething().u{caret}
    using (var coolType = GetSomething()) { }
  }
}