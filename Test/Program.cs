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
            ParserReturnState result = Translator.CallMe("(Int -> Int) werk bitch = asd\nChar thisthat = 1");
        }
    }
}
