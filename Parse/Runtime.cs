using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace Parse
{
    public class PExpression
    {
        public string Identifier { get; protected set; }
        public dynamic Value { get; private set; }
        public TypeSignature TypeSignature { get; protected set; }

        protected Stack<(PExpression, int)> SubExpressions { get; }

        private readonly bool isParamater = false;
        private int parameterIndex;

        private PExpressionType type = PExpressionType.Definition;

        private readonly bool isBaseExpression = false;
        private readonly Stack<PExpression> arguments;
        private readonly Func<Stack<PExpression>, string, PExpression> function;

        //Instantiates expression representing data value
        public PExpression(dynamic value, string identifier = "")
        {
            Identifier = identifier;
            Value = value;
            type = PExpressionType.Evaluated;
            TypeSignature = new TypeSignature(value.GetType());
        }

        //Instatiates base function definition
        public PExpression(Func<Stack<PExpression>, string, PExpression> function, TypeSignature typeSignature, string identifier = "")
        {
            Identifier = identifier;
            TypeSignature = typeSignature;
            arguments = new Stack<PExpression>();
            isBaseExpression = true;
            this.function = function;
        }

        //Instantiates function definition (function should be constructed using SubExpressions queue instantiated here)
        public PExpression(TypeSignature typeSignature, string identifier = "")
        {
            Identifier = identifier;
            TypeSignature = typeSignature;
            SubExpressions = new Stack<(PExpression, int)>();
        }

        //Instantiates paramater of function (would be added to SubExpressions queue of function definition)
        public PExpression(int parameterIndex, TypeSignature typeSignature, string identifier = "")
        {
            isParamater = true;
            Identifier = identifier;
            TypeSignature = typeSignature;
            this.parameterIndex = parameterIndex;
        }

        public void PushSubExpression(PExpression subExpression, int argumentCount)
        {
            SubExpressions.Push((subExpression, argumentCount));
        }

        public PExpression Evaluate()
        {
            if (type == PExpressionType.Evaluated)
            {
                return this;
            }
            //Doesn't matter here if the expression is a definition and doesn't need to be cloned, as it is a separately defined expression that
            //once evaluated can remain as an evaluated expression or definition and be used in multiple places without being re-evaluated
            else
            {
                Stack<PExpression> tempStack = new Stack<PExpression>();
                while (SubExpressions.Count > 0)
                {
                    try
                    {
                        (PExpression expression, int argumentCount) workingExpressionTuple = SubExpressions.Pop();
                        PExpression workingExpression = workingExpressionTuple.expression;
                        if (workingExpressionTuple.argumentCount > 0)
                        {
                            for (int i = 0; i < workingExpressionTuple.argumentCount; i++)
                            {
                                workingExpression = workingExpression.Evaluate(tempStack.Pop());
                            }
                        }
                        else
                        {
                            workingExpression = workingExpression.Evaluate();
                        }
                        tempStack.Push(workingExpression);
                    }
                    catch
                    {
                        throw new PaskellRuntimeException("Failure trying to evaluate expression", this);
                    }
                }

                PExpression result = tempStack.Pop();

                if (!result.TypeSignature.IsFunction)
                {
                    result.type = PExpressionType.Evaluated;
                }
                else
                {
                    throw new PaskellRuntimeException("Trying to find the value of unevaluated expression", this);
                }
                return result;
            }
        }

        public PExpression Evaluate(PExpression argument)
        {
            //Important here however if that the expression is an definition, it remains unchanged and a new instance
            //of the class is created as the worked expression, protecting the function definition
            PExpression workedExpression = CloneWorkedExpression();     //Only clones if not already worked expression (handled within method)
            if (!isBaseExpression)
            {
                if (workedExpression.type == PExpressionType.WorkedExpression)
                {
                    Stack<(PExpression, int)> tempStack = new Stack<(PExpression, int)>();
                    while (workedExpression.SubExpressions.Count > 0)
                    {
                        (PExpression expression, int argumentCount) expression = workedExpression.SubExpressions.Pop();
                        if (expression.expression.isParamater)
                        {
                            if (expression.expression.parameterIndex == 0)
                            {
                                expression.expression = argument;
                            }
                            else
                            {
                                expression.expression.parameterIndex--;
                            }
                        }
                        tempStack.Push(expression);
                    }
                    while (tempStack.Count > 0)
                    {
                        workedExpression.SubExpressions.Push(tempStack.Pop());
                    }
                    workedExpression.TypeSignature = workedExpression.TypeSignature.Return;

                    if (!workedExpression.TypeSignature.IsFunction)
                    {
                        workedExpression = workedExpression.Evaluate();
                    }
                }
                else
                {
                    throw new PaskellRuntimeException("Trying to pass argument to non-function expression", this);
                }

                return workedExpression;
            }
            else
            {
                workedExpression.arguments.Push(argument);
                workedExpression.TypeSignature = workedExpression.TypeSignature.Return;
                if (!workedExpression.TypeSignature.IsFunction)
                {
                    Stack<PExpression> arguments = new Stack<PExpression>();
                    while (workedExpression.arguments.Count > 0)
                    {
                        arguments.Push(workedExpression.arguments.Pop());
                    }
                    try
                    {
                        PExpression result = function(arguments, workedExpression.Identifier);
                        workedExpression = result;
                    }
                    catch
                    {
                        throw new PaskellRuntimeException("Failure trying to evaluate base expression", workedExpression);
                    }
                }

                return workedExpression;
            }
        }

        protected PExpression CloneWorkedExpression()
        {
            PExpression workedExpression;

            if (type == PExpressionType.Definition)
            {
                if (isParamater)
                {
                    workedExpression = new PExpression(parameterIndex, TypeSignature, Identifier);
                }
                else if (isBaseExpression)
                {
                    workedExpression = new PExpression(function, TypeSignature, Identifier);
                }
                else
                {
                    workedExpression = new PExpression(TypeSignature, Identifier);
                    Stack<(PExpression, int)> tempStack = new Stack<(PExpression, int)>();
                    while (SubExpressions.Count > 0)
                    {
                        tempStack.Push(SubExpressions.Pop());
                    }
                    while (tempStack.Count > 0)
                    {
                        var subExpression = tempStack.Pop();
                        workedExpression.SubExpressions.Push((subExpression.Item1.CloneWorkedExpression(), subExpression.Item2));
                        SubExpressions.Push(subExpression);
                    }
                }
                workedExpression.type = PExpressionType.WorkedExpression;
            }
            else
            {
                workedExpression = this;
            }

            return workedExpression;
        }
    }

    public class TypeSignature
    {
        public bool IsFunction { get; }
        public Type Type { get; }                   // --Used if IsFunction is false
        public TypeSignature Parameter { get; }     // --Used if IsFunction is true
        public TypeSignature Return { get; }        // _/

        public string Value => ToString(); //Mostly for debug purposes

        public int ArgumentCount
        {
            get
            {
                if (IsFunction)
                {
                    return 1 + Return.ArgumentCount;
                }
                else
                {
                    return 0;
                }
            }
        }

        public TypeSignature FinalType
        {
            get
            {
                if (IsFunction)
                {
                    return Return.FinalType;
                }
                else
                {
                    return this;
                }
            }
        }

        public bool ContainsGenericTypeSignatures
        {
            get
            {
                if (IsFunction)
                {
                    return Parameter.ContainsGenericTypeSignatures || Return.ContainsGenericTypeSignatures;
                }
                else if (!IsFunction && Type == null)
                {
                    return true;
                }
                else
                {
                    return false;
                }

            }
        }

        public TypeSignature this[int i]
        {
            get
            {
                if (i > ArgumentCount || i < 0)
                {
                    throw new IndexOutOfRangeException();
                }
                else
                {
                    if (i == 0)
                    {
                        return this;
                    }
                    else
                    {
                        return Return[i - 1];
                    }
                }
            }
        }

        public override bool Equals(object obj)
        {
            //Check for null and compare run-time types.
            if ((obj == null) || !this.GetType().Equals(obj.GetType()))
            {
                return false;
            }
            else
            {
                TypeSignature typeSignature = (TypeSignature)obj;
                if (IsFunction && typeSignature.IsFunction)
                {
                    return Parameter == typeSignature.Parameter && Return == typeSignature.Return;
                }
                else if (!IsFunction && !typeSignature.IsFunction && Type != null)
                {
                    return Type == typeSignature.Type;
                }
                else
                {
                    return false;
                }
            }
        }

        public static bool operator == (TypeSignature t1, TypeSignature t2)
        {
            return ReferenceEquals(t1, null) && ReferenceEquals(t2, null) || t1.Equals(t2);
        }

        public static bool operator !=(TypeSignature t1, TypeSignature t2)
        {
            return !(t1 == t2);
        }

        //Instantiates generic type signature, unspecified type
        public TypeSignature()
        {
            IsFunction = false;
        }

        //Instantiates type signature of a variable of type given
        public TypeSignature(Type type)
        {
            IsFunction = false;
            Type = type;
            GetTypeString(type);
        }

        //Instantiates type signature of function with given parameter and return signatures
        public TypeSignature(TypeSignature parameter, TypeSignature returnt)
        {
            IsFunction = true;
            Parameter = parameter;
            Return = returnt;
        }

        public string ToString(bool brackets = true)
        {
            return IsFunction ? $"{(brackets ? "(" : "")}{Parameter.ToString()} -> {Return.ToString(false)}{(brackets ? ")" : "")}" : (Type != null ? GetTypeString(Type) : "_");
        }

        private string GetTypeString(Type type)
        {
            foreach (OperandType operandType in Enum.GetValues(typeof(OperandType)))
            {
                if (operandType.GetPType() == type)
                {
                    return operandType.ToString();
                }
            }

            throw new InvalidOperationException("Not valid type");
        }
    }

    enum PExpressionType
    {
        Definition,
        WorkedExpression,
        Evaluated
    }
}