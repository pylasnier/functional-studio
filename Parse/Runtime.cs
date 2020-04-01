using System;
using System.Collections.Generic;

namespace Parse
{
    public class PExpression
    {
        public string Identifier { get; protected set; }
        public dynamic Value { get; private set; }      //Used if the expression is an evaluated variable
        public TypeSignature TypeSignature { get; protected set; }

        //Used if the expression is an unevaluated expression i.e. an unevaluated variable or a function definition
        protected Stack<(PExpression, (int, Stack<ConditionSpecifier>))> SubExpressions { get; }

        //Used if the expression is a parameter
        private readonly bool isParamater = false;
        private int parameterIndex;

        private PExpressionState state = PExpressionState.Definition;   //Definitions should never be directly worked on; they must be cloned first

        //Variables used if the expresion is a base expression
        private readonly bool isBaseExpression = false;
        private readonly Stack<PExpression> arguments;
        private readonly Func<Stack<PExpression>, PExpression> function;

        //Instantiates expression representing data value
        public PExpression(dynamic value, string identifier = "")
        {
            Identifier = identifier;
            Value = value;
            state = PExpressionState.Evaluated;
            TypeSignature = new TypeSignature(value.GetType());
        }

        //Instatiates base function definition
        public PExpression(Func<Stack<PExpression>, PExpression> function, TypeSignature typeSignature, string identifier = "")
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
            SubExpressions = new Stack<(PExpression, (int, Stack<ConditionSpecifier>))>();
        }

        //Instantiates paramater of function (would be added to SubExpressions queue of function definition)
        public PExpression(int parameterIndex, TypeSignature typeSignature, string identifier = "")
        {
            isParamater = true;
            Identifier = identifier;
            TypeSignature = typeSignature;
            this.parameterIndex = parameterIndex;
        }

        //Since SubExpressions is a protected property, it needs a public accessor to push items
        public void PushSubExpression(PExpression subExpression, int argumentCount, Stack<ConditionSpecifier> conditionSpecifiers)
        {
            SubExpressions.Push((subExpression, (argumentCount, conditionSpecifiers)));
        }

        //This function is called when the value of an expression is trying to be retrieved
        //The expression must therefore be a variable or a parameterised function
        public PExpression Evaluate()
        {
            if (state == PExpressionState.Evaluated)
            {
                return this;
            }
            else
            {
                PExpression workedExpression = CloneWorkedExpression();
                PExpression result = EvaluateSubExpressions(workedExpression.SubExpressions);

                if (!result.TypeSignature.IsFunction)
                {
                    result.state = PExpressionState.Evaluated;
                }
                else
                {
                    throw new PaskellRuntimeException("Trying to find the value of unevaluated expression", result);
                }
                return result;
            }
        }

        private PExpression EvaluateSubExpressions(Stack<(PExpression, (int, Stack<ConditionSpecifier>))> subExpressions)
        {
            Stack<PExpression> workingStack = new Stack<PExpression>();
            EvaluateSubExpressions(new Queue<(PExpression, (int, Stack<ConditionSpecifier>))>(subExpressions), workingStack);
            //Using the working stack, the final result of the evaluated subexpressions should be the only element on the stack, which can then be popped
            return workingStack.Pop();
        }

        private void EvaluateSubExpressions(Queue<(PExpression, (int, Stack<ConditionSpecifier>))> subExpressions, Stack<PExpression> workingStack)
        {
            //These queues used as temporary holding of subexpressions, to ensure that only the necessary subexpressions are evaluated depending on a condition
            Queue<(PExpression, (int, Stack<ConditionSpecifier>))> condition = new Queue<(PExpression, (int, Stack<ConditionSpecifier>))>();
            Queue<(PExpression, (int, Stack<ConditionSpecifier>))> ifTrue = new Queue<(PExpression, (int, Stack<ConditionSpecifier>))>();
            Queue<(PExpression, (int, Stack<ConditionSpecifier>))> ifFalse = new Queue<(PExpression, (int, Stack<ConditionSpecifier>))>();

            while (subExpressions.Count > 0)
            {
                try
                {
                    (PExpression expression, (int argumentCount, Stack<ConditionSpecifier> conditionSpecifiers) evaluationSpecifiers) workingExpressionTuple = subExpressions.Dequeue();
                    PExpression expression = workingExpressionTuple.expression;
                    //This copy is necessary as the count may change within the loop, but the initial count must be remembered
                    int conditionSpecifierCount = workingExpressionTuple.evaluationSpecifiers.conditionSpecifiers.Count;
                    if (conditionSpecifierCount != 0)
                    {
                        ConditionSpecifier conditionSpecifier = workingExpressionTuple.evaluationSpecifiers.conditionSpecifiers.Pop();
                        switch (conditionSpecifier)
                        {
                            case ConditionSpecifier.Condition:
                                condition.Enqueue(workingExpressionTuple);
                                break;

                            case ConditionSpecifier.IfTrue:
                                ifTrue.Enqueue(workingExpressionTuple);
                                break;

                            case ConditionSpecifier.IfFalse:
                                //Condition clause would only ever be preceded by an else clause, or a non-condition subexpression
                                //This is for the case of a condition clause being preceded by an else clause
                                if (condition.Count != 0)
                                {
                                    EvaluateSubExpressions(condition, workingStack);
                                    bool conditionValue = workingStack.Pop().Evaluate().Value;
                                    if (conditionValue == true)
                                    {
                                        EvaluateSubExpressions(ifTrue, workingStack);
                                        ifFalse.Clear();
                                    }
                                    else
                                    {
                                        EvaluateSubExpressions(ifFalse, workingStack);
                                        ifTrue.Clear();
                                    }
                                }
                                ifFalse.Enqueue(workingExpressionTuple);
                                break;
                        }
                    }
                    //This is for the case of a condition clause being preceded by a non-condition subexpression or the start of the subexpressions
                    if (condition.Count != 0 && (conditionSpecifierCount == 0 || subExpressions.Count == 0))
                    {
                        //The bool value of the condition must be evaluated, then the correct clause evaluated according to that
                        //When recursively calling EvaluateSubExpressions here, nested condition blocks are considered as the top condition specifier is popped above
                        EvaluateSubExpressions(condition, workingStack);
                        bool conditionValue = workingStack.Pop().Evaluate().Value;
                        if (conditionValue == true)
                        {
                            EvaluateSubExpressions(ifTrue, workingStack);
                            ifFalse.Clear();
                        }
                        else
                        {
                            EvaluateSubExpressions(ifFalse, workingStack);
                            ifTrue.Clear();
                        }
                    }
                    //If there is no condition specified, then the subexpression can just be normally evaluated
                    if (conditionSpecifierCount == 0)
                    {
                        for (int i = 0; i < workingExpressionTuple.evaluationSpecifiers.argumentCount; i++)
                        {
                            expression = expression.Evaluate(workingStack.Pop());
                        }
                        if (!expression.TypeSignature.IsFunction)
                        {
                            expression = expression.Evaluate();
                        }
                        workingStack.Push(expression);
                    }
                }
                catch
                {
                    throw new PaskellRuntimeException("Failed to evaluate expression", this);
                }
            }
        }

        //Called when passing an argument to a function
        public PExpression Evaluate(PExpression argument)
        {
            //Important here however if that the expression is an definition, it remains unchanged and a new instance
            //of the class is created as the worked expression, protecting the function definition
            PExpression workedExpression = CloneWorkedExpression();     //Only clones if not already worked expression (handled within method)
            if (!isBaseExpression)
            {
                if (workedExpression.state == PExpressionState.WorkedExpression)
                {
                    //Using a temp stack effectively allows looping through a stack by transferring items to the temp stack, then back to the original once done
                    Stack<(PExpression, (int, Stack<ConditionSpecifier>))> tempStack = new Stack<(PExpression, (int, Stack<ConditionSpecifier>))>();
                    while (workedExpression.SubExpressions.Count > 0)
                    {
                        (PExpression expression, (int, Stack<ConditionSpecifier>)) expression = workedExpression.SubExpressions.Pop();
                        if (expression.expression.isParamater)
                        {
                            //It is important to clone the parameter, else each of a parameter in an expression where there are multiple will be changed
                            expression.expression = expression.expression.CloneWorkedExpression();
                            //Parameters are indexed, 0 upwards, where the highest numbered are for the final argument
                            if (expression.expression.parameterIndex == 0)
                            {
                                expression.expression = argument;
                            }
                            else
                            {
                                //So that for the next argument passed, the next index i.e. index 1 will be replaced
                                //This is done by decrementing all indicies, so 1 becomes 0 etc.
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
                        //Function fully parameterised
                        workedExpression = workedExpression.Evaluate();
                    }
                }
                else
                {
                    throw new PaskellRuntimeException("Trying to pass argument to non-function expression", workedExpression);
                }

                return workedExpression;
            }
            //For if the expression is a base expression
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
                        //Calls the function of the base expression and passes it the stack of arguments passed
                        PExpression result = function(arguments).CloneWorkedExpression(workedExpression.Identifier);    //Calling clone function just allows the identifier to be assigned
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

        //When compiled, every function definition is classed as a definition, which indicates that it represents the original definition of any expression.
        //It is important that when evaluating expressions, the original definition remains untouched as a function or expression may be referenced
        //in multiple places, for example in recursion. Therefore a clone must be used instead, and it is identified as a worked expression which is
        //safe to work on
        protected PExpression CloneWorkedExpression(string identifier = null)
        {
            PExpression workedExpression;
            
            if (identifier == null)
            {
                identifier = Identifier;
            }

            if (state == PExpressionState.Definition)
            {
                if (isParamater)
                {
                    workedExpression = new PExpression(parameterIndex, TypeSignature, identifier);
                }
                else if (isBaseExpression)
                {
                    workedExpression = new PExpression(function, TypeSignature, identifier);
                }
                else
                {
                    workedExpression = new PExpression(TypeSignature, identifier);
                    Stack<(PExpression, (int, Stack<ConditionSpecifier>))> tempStack = new Stack<(PExpression, (int, Stack<ConditionSpecifier>))>();
                    while (SubExpressions.Count > 0)
                    {
                        tempStack.Push(SubExpressions.Pop());
                    }
                    while (tempStack.Count > 0)
                    {
                        //If subexpressions contain references to other expression definitions, they shouldn't be cloned until they have to be evaluated
                        //Therefore they are left alone, and will naturally be cloned when evaluated i.e. this function gets called from their Evaluate call
                        (PExpression expression, (int argumentCount, Stack<ConditionSpecifier> conditionSpecifiers) evaluationSpecifiers) subExpression = tempStack.Pop();
                        PExpression workedSubExpression = subExpression.expression;
                        if (workedSubExpression.state != PExpressionState.Definition)
                        {
                            workedSubExpression = workedSubExpression.CloneWorkedExpression();
                        }
                        //Important to clone the evaluation specifier stacks too
                        workedExpression.SubExpressions.Push((workedSubExpression, (subExpression.evaluationSpecifiers.argumentCount,
                                                                new Stack<ConditionSpecifier>(new Stack<ConditionSpecifier>(subExpression.evaluationSpecifiers.conditionSpecifiers)))));
                        SubExpressions.Push(subExpression);
                    }
                }
                workedExpression.state = PExpressionState.WorkedExpression;
            }
            //If already cloned
            else
            {
                workedExpression = this;
                workedExpression.Identifier = identifier;
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

        public string Value => ToString(); //Mostly for debug purposes, writes type signature in Paskell syntax

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

        //Every next index of a type signature is just that type signature's return
        //From this, every argument type of a type signature can be indexed as TypeSignature[i].Parameter
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

        //Used to check if type signatures are the same (generic types count as equalling anything)
        //Also necessary to allow comparison with null if uninstantiated, though I don't think I've fully implemented a solution for this
        //However it works for my limited use of comparisons
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
                else if (!IsFunction && !typeSignature.IsFunction)
                {
                    if (Type == null || typeSignature.Type == null)
                    {
                        return true;
                    }
                    else
                    {
                        return Type == typeSignature.Type;
                    }
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

        //Instantiates generic variable type
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

        //Produces Paskell syntax type signature
        public string ToString(bool brackets = true)
        {
            return IsFunction ? $"{(brackets ? "(" : "")}{Parameter.ToString()} -> {Return.ToString(false)}{(brackets ? ")" : "")}" : (Type != null ? GetTypeString(Type) : "_");
        }

        //Ensures that the types given by the enum are returned as strings rather than the C# types
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

    enum PExpressionState
    {
        Definition,
        WorkedExpression,
        Evaluated
    }

    public enum ConditionSpecifier
    {
        Condition,
        IfTrue,
        IfFalse
    }
}