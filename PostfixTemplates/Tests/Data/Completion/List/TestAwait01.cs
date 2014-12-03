using System.Threading.Tasks;

class Playground
{
  async void Foo(Task<bool> task)
  {
    task.ConfigureAwait(false).a{caret}
  }
}