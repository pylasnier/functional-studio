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
            add.Function = Add;
            add.ArgumentCount = 2;
            add.TypeSignature = new TypeSignature(new TypeSignature(), new TypeSignature());

            PExpression test = new PExpression() { TypeSignature = new TypeSignature(new TypeSignature(typeof(int)), new TypeSignature(new TypeSignature(typeof(int)), new TypeSignature(typeof(int)))) };
            test.SubExpressions.Enqueue(add);
            test.SubExpressions.Enqueue(new PExpression() { TypeSignature = new TypeSignature(typeof(int)), IsParameter = true, ParameterIndex = 0 });
            test.SubExpressions.Enqueue(new PExpression() { TypeSignature = new TypeSignature(typeof(int)), IsParameter = true, ParameterIndex = 1 });
            test = test.Evaluate(new PExpression() { TypeSignature = new TypeSignature(typeof(int)), Value = 7, Evaluated = true });
            test = test.Evaluate(new PExpression() { TypeSignature = new TypeSignature(typeof(int)), Value = 4, Evaluated = true });
        }

        private static PExpression Add(Queue<PExpression> a)
        {
            var result = a.Dequeue().Evaluate().Value + a.Dequeue().Evaluate().Value;
            return new PExpression() { Value = result, TypeSignature = new TypeSignature(result.GetType()), Evaluated = true };
        }
    }
}
