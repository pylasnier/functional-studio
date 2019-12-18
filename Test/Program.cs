using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utility;

namespace Test
{
    class Program
    {
        static void Main(string[] args)
        {
            int[] test = {4, 8, 1, 9, 7, 6, 6, 4, 9, 4, 9, 5};

            Console.WriteLine(string.Join(", ", Tools.MergeSort(test)));
        }
    }
}
