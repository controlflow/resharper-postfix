public class ModelClass
{
  public bool IsValid { get; set; }
  public object Value { get; set; }
  public int[] Items { get; set; }
  public System.IDisposable Form { get; set; }
  public MyEnum EnumProperty { get; set; }
}

public enum MyEnum
{
  SomeCase
}