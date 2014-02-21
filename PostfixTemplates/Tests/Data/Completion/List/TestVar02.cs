using System.Text;

public class Foo
{
  public string Bar()
  {
    return new StringBuilder().{caret}.ToString();
  }
}