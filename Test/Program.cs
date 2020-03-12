using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utility;
using Parse;

namespace Test
{
    class Program
    {
        static void Main(string[] args)
        {
            Translator.CallMe("Int -> Int MyAdd a = Add 3 a\nBool -> Int MyIf a = MyAdd (IfThenElse a 1 2)\nInt main = MyAdd false");
        }
    }
}
