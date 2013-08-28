using FastestEnumerator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tester
{
    class Program
    {
        static void Main(string[] args)
        {
            List<FileData> files = FastEnumerator.EnumerateFiles(@"C:\Windows\System32", "*").ToList();
            Console.ReadKey();
        }
    }
}
