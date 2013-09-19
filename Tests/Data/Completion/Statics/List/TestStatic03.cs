using System.Threading;

class Foo
{
  void Bar()
  {
    Thread.CurrentThread.{caret}
  }
}