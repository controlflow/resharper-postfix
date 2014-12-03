using System.Threading.Tasks;

class Playground
{
  async void Foo(Task task)
  {
    task.ConfigureAwait(false).a{caret}
  }
}