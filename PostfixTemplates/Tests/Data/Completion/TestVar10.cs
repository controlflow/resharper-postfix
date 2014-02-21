// ${COMPLETE_ITEM:var}

using System;
using System.Linq;

class Program
{
  static void Main(string[] args)
  {
    var ts = from x in args
             select x.v{caret} + 1;
  }
}