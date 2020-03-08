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
            PExpression first = new PExpression();
            first.Value = 7;
            PExpression second = new PExpression();
            second.Value = 4;

            PExpression add = new PExpression();
            add.IsBaseExpression = true;
            add.Function = (Queue<dynamic> a) => (a.Dequeue() + a.Dequeue());
            add.ArgumentCount = 2;

            PExpression test = new PExpression() { TypeSignature = new TypeSignature { IsFunction = true, Parameter = new TypeSignature { IsFunction = false, Type = typeof(int) }, Return = new TypeSignature { IsFunction = true, Parameter = new TypeSignature { IsFunction = false, Type = typeof(int) }, Return = new TypeSignature { IsFunction = false, Type = typeof(int) } } } };
            test.SubExpressions.Enqueue(add);
            test.SubExpressions.Enqueue(new PExpression() { TypeSignature = new TypeSignature { IsFunction = false, Type = typeof(int) }, IsArgument = true, ArgumentIndex = 0 }); ;
            test.SubExpressions.Enqueue(new PExpression() { TypeSignature = new TypeSignature { IsFunction = false, Type = typeof(int) }, IsArgument = true, ArgumentIndex = 1 });
            test = test.Evaluate(new PExpression() { TypeSignature = new TypeSignature { IsFunction = false, Type = typeof(int) }, Value = 7, Evaluated = true });
            test = test.Evaluate(new PExpression() { TypeSignature = new TypeSignature { IsFunction = false, Type = typeof(int) }, Value = 4, Evaluated = true });
        }
    }
}
