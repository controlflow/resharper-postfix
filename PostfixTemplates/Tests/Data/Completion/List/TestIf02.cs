using System.Threading.Tasks;

class Playground
{
  async void Foo(Task<bool> task)
  {
    await task.ConfigureAwait(false).i{caret}
  }
}