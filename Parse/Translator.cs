using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Reflection;
using Utility;

namespace Parse
{
    public static class Translator
    {
        public static ParserReturnState CallMe(string code)
        {
            Token[] output;
            PContext context;
            Tokenise(code, out output);
            return Compile(output, out context);
        }

        private static ParserReturnState Tokenise(string sourceCode, out Token[] TokenCode)
        {
            List<Token> tokenCollection = new List<Token>();
            bool[] codeMatched = new bool[sourceCode.Length];
            ParserReturnState returnState;
            MatchCollection matches;

            codeMatched.Populate(false, 0, codeMatched.Length);

            //Loops through every token for their regex patterns, as given by their RegexPattern custom attribute
            foreach (var tokenType in Enum.GetValues(typeof(TokenType)))
            {
                matches = new Regex(((TokenType) tokenType).GetPattern()).Matches(sourceCode);
                foreach (Match match in matches)
                {
                    tokenCollection.Add(new Token(match.Value, (TokenType) tokenType, match.Index));
                    codeMatched.Populate(true, match.Index, match.Length);
                }
            }

            //Finding whitespace just to fill codeMatched, so that it doesn't detect spaces as syntax errors
            matches = new Regex(@"\s").Matches(sourceCode);
            foreach (Match match in matches)
            {
                codeMatched.Populate(true, match.Index, match.Length);
            }

            //Main useful output, the tokenised code
            TokenCode = tokenCollection.ToArray();
            TokenCode.Sort();

            //Error output, indicates success or partial failure and errors if there are failures
            if (codeMatched.Any(element => element == false))
            {
                returnState = new ParserReturnState(false);
                //Loops through to find each occurence of an error
                for (int i = 0; i < codeMatched.Length; /*Increments handled in loop*/)
                {
                    if (codeMatched[i] == false)
                    {
                        returnState.Errors.Push(new ParserReturnErrorInfo(ParserReturnError.BadIdentifier, i));
                        while (i < codeMatched.Length && codeMatched[i] == false) i++;
                    }
                    else i++;
                }
            }
            else
            {
                returnState = new ParserReturnState(true);
            }

            return returnState;
        }

        private static ParserReturnState Compile(Token[] tokenCode, out PContext Context)
        {
            ParserReturnState returnState = new ParserReturnState();

            List<Token[]> lines = new List<Token[]>();

            List<PExpression> Expressions = new List<PExpression>();
            Expressions.Add(new PExpression(Add, new TypeSignature(new TypeSignature(), new TypeSignature(new TypeSignature(), new TypeSignature())), 2, "Add"));
            Expressions.Add(new PExpression(IfThenElse, new TypeSignature(new TypeSignature(typeof(bool)), new TypeSignature(new TypeSignature(), new TypeSignature(new TypeSignature(), new TypeSignature()))), 3, "IfThenElse"));

            bool[] codeMatched = new bool[tokenCode.Length];

            ParserState parserState = ParserState.TypeSignature;

            int startIndex = 0;
            int endIndex;

            //Divided code into lines
            Token[] copyLine;
            for (int i = 0; i < tokenCode.Length; i++)
            {
                if (tokenCode[i].TokenType == TokenType.LineBreak)
                {
                    endIndex = i;
                    copyLine = new Token[endIndex - startIndex];
                    Array.Copy(tokenCode, startIndex, copyLine, 0, copyLine.Length);
                    lines.Add(copyLine);
                    startIndex = endIndex + 1;
                }
            }
            endIndex = tokenCode.Length;
            copyLine = new Token[endIndex - startIndex];
            Array.Copy(tokenCode, startIndex, copyLine, 0, copyLine.Length);
            lines.Add(copyLine);

            //Instantiate expressions
            foreach (Token[] line in lines)
            {
                try
                {
                    bool wasLastWord = false;
                    int expressionSignatureIndex = 0;
                    int equateIndex = 0;

                    //Finding points to split at for type signature, expression signature, and expression definition
                    for (int i = 0; i < line.Length; i++)
                    {
                        if (expressionSignatureIndex == 0)
                        {
                            if (line[i].TokenType == TokenType.Word)
                            {
                                if (wasLastWord)
                                {
                                    expressionSignatureIndex = i;
                                }
                                else
                                {
                                    wasLastWord = true;
                                }
                            }
                            else if (line[i].TokenType != TokenType.Bracket)
                            {
                                wasLastWord = false;
                            }
                        }
                        else if (equateIndex == 0)
                        {
                            if (line[i].TokenType == TokenType.Equate)
                            {
                                equateIndex = i;
                                break;
                            }
                        }
                    }

                    if (expressionSignatureIndex == 0)
                    {
                        throw new PaskellCompileException("No signature for expression given", line.Length);
                    }
                    if (equateIndex == 0)
                    {
                        throw new PaskellCompileException("Expression not set to anything", line.Length);
                    }

                    //Getting type signature and expression signature
                    Token[] subTokens = new Token[expressionSignatureIndex];
                    Array.Copy(line, 0, subTokens, 0, subTokens.Length);
                    TypeSignature typeSignature = ConstructTypeSignature(subTokens);

                    subTokens = new Token[equateIndex - expressionSignatureIndex];
                    Array.Copy(line, expressionSignatureIndex, subTokens, 0, subTokens.Length);
                    if (subTokens.Length == 0 || subTokens[0].TokenType != TokenType.Word)
                    {
                        throw new PaskellCompileException("Expected expression identifier", expressionSignatureIndex);
                    }
                    PExpression pExpression = new PExpression(typeSignature, subTokens[0].Code);
                    Expressions.Add(pExpression);
                }
                catch (PaskellCompileException e)
                {

                }
            }

            Context =  new PContext(Expressions.ToArray());
            return null;
        }

        private static TypeSignature ConstructTypeSignature(Token[] tokenCode)
        {
            int bracketNesting = 0;
            int bracketStartIndex = 0;
            int bracketEndIndex = 0;
            int functionMapIndex = 0;

            TypeSignature typeSignature = null;
            TypeSignature left = null;
            bool isFunction = false;

            if (tokenCode.Length == 0)
            {
                throw new PaskellCompileException("Expected type", 0);
            }

            for (int i = 0; i < tokenCode.Length; i++)
            {
                Token token = tokenCode[i];
                if (token.TokenType == TokenType.Bracket)
                {
                    if (token.Code == "(")
                    {
                        if (bracketNesting == 0)
                        {
                            bracketStartIndex = i + 1;
                        }
                        bracketNesting++;
                    }
                    else
                    {
                        if (bracketNesting == 0)
                        {
                            throw new PaskellCompileException("Unexpected bracket", i);
                        }
                        else
                        {
                            bracketEndIndex = i;
                            bracketNesting--;
                        }
                    }
                }
                else if (bracketNesting == 0)
                {
                    if (token.TokenType == TokenType.FunctionMap)
                    {
                        isFunction = true;
                        functionMapIndex = i;
                        if (bracketEndIndex == 0)
                        {
                            bracketEndIndex = i;
                        }
                        Token[] newTokenCode = new Token[bracketEndIndex - bracketStartIndex];
                        Array.Copy(tokenCode, bracketStartIndex, newTokenCode, 0, newTokenCode.Length);
                        try
                        {
                            left = ConstructTypeSignature(newTokenCode);
                        }
                        catch (PaskellCompileException e)
                        {
                            throw new PaskellCompileException(e.ErrorMessage, e.Index + bracketStartIndex);
                        }
                        break;
                    }
                }
            }

            if (bracketNesting > 0)
            {
                throw new PaskellCompileException("Expected bracket", tokenCode.Length - 1);
            }

            if (isFunction)
            {
                Token[] newTokenCode = new Token[tokenCode.Length - functionMapIndex - 1];
                Array.Copy(tokenCode, functionMapIndex + 1, newTokenCode, 0, newTokenCode.Length);
                try
                {
                    typeSignature = new TypeSignature(left, ConstructTypeSignature(newTokenCode));
                }
                catch (PaskellCompileException e)
                {
                    throw new PaskellCompileException(e.ErrorMessage, e.Index + functionMapIndex + 1);
                }

            }
            else
            {
                if (bracketEndIndex != 0)
                {
                    Token[] newTokenCode = new Token[tokenCode.Length - 2];
                    Array.Copy(tokenCode, 1, newTokenCode, 0, newTokenCode.Length);
                    typeSignature = ConstructTypeSignature(newTokenCode);
                }
                else if (tokenCode.Length > 1)
                {
                    throw new PaskellCompileException("Too many expressions", 0);
                }
                else
                {
                    if (tokenCode[0].TokenType != TokenType.Word)
                    {
                        throw new PaskellCompileException("Expected type", 0);
                    }
                    foreach (OperandType operandType in Enum.GetValues(typeof(OperandType)))
                    {
                        if (tokenCode[0].Code == operandType.ToString())
                        {
                            typeSignature = new TypeSignature(operandType.GetPType());
                            break;
                        }
                    }
                    if (typeSignature == null)
                    {
                        throw new PaskellCompileException("Invalid type", 0);
                    }
                }
            }

            return typeSignature;
        }

        //private static ParserReturnState Compiles(Token[] tokenCode, out PContext Context)
        //{
        //    List<PExpression> myExpressions = new List<PExpression>();
        //    bool[] codeMatched = new bool[tokenCode.Length];
        //    ParserReturnState returnState;

        //    codeMatched.Populate(false, 0, tokenCode.Length);

        //    for (int i = 0; i < tokenCode.Length; /*Increment handled inside of loop*/)
        //    {
        //        ParserState parserState = ParserState.TypeSignature;

        //        PExpression myExpression;
        //        TypeSignature typeSignature;
        //        bool isFunction = false;
        //        Type type = null;

        //        if (tokenCode[i].TokenType == TokenType.Word)
        //        {
        //            foreach (OperandType operandType in Enum.GetValues(typeof(OperandType)))
        //            {
        //                if (tokenCode[i].Code == operandType.ToString())
        //                {
        //                    type = operandType.GetPType();
        //                    codeMatched[i] = true;
        //                    i++;
        //                    break;
        //                }
        //            }
        //        }

        //        if (type != null)
        //        {
        //            if (tokenCode[i].TokenType == TokenType.Word)
        //            {
        //                myExpression.Identifier = tokenCode[i].Code;
        //                i++;

        //                if (tokenCode[i].TokenType == TokenType.Equate)
        //                {
        //                    i++;

        //                    if (tokenCode[i].TokenType == TokenType.Operand)
        //                    {
        //                        var converter = TypeDescriptor.GetConverter(type);
        //                        try
        //                        {
        //                            myExpression.Value = converter.ConvertFrom(tokenCode[i].Code);
        //                            myExpressions.Add(myExpression);
        //                            codeMatched[i] = true;
        //                        }
        //                        catch { }
        //                    }
        //                }
        //            }
        //        }

        //        i++;
        //    }

        //    Context = new PContext(myExpressions.ToArray());

        //    if (codeMatched.Any(element => element == false))
        //    {
        //        returnState = new ParserReturnState(false);
        //        for (int i = 0; i < codeMatched.Length; i++)
        //        {
        //            if (codeMatched[i] == false)
        //            {
        //                returnState.Errors.Push(new ParserReturnErrorInfo(ParserReturnError.BadIdentifier, i));
        //            }
        //        }
        //    }
        //    else
        //    {
        //        returnState = new ParserReturnState(true);
        //    }

        //    return returnState;
        //}

        private static PExpression Add(Stack<PExpression> a)
        {
            var result = a.Pop().Evaluate().Value + a.Pop().Evaluate().Value;
            return new PExpression(result, "result");
        }

        private static PExpression IfThenElse(Stack<PExpression> a)
        {
            bool comparison = a.Pop().Evaluate().Value;
            PExpression ifTrue = a.Pop();
            PExpression ifFalse = a.Pop();
            if (comparison)
            {
                return ifTrue;
            }
            else
            {
                return ifFalse;
            }
        }
    }

    //https://stackoverflow.com/questions/479410/enum-tostring-with-user-friendly-strings
    static class CustomEnumExtensions
    {
        public static string GetPattern(this TokenType enumValue)
        {
            string pattern = "";
            MemberInfo memberInfo = enumValue.GetType().GetMember(enumValue.ToString())[0];
            foreach (var customAttribute in memberInfo.GetCustomAttributes(typeof(RegexPattern), false))
            {
                pattern = ((RegexPattern) customAttribute).Pattern;
            }

            return pattern;
        }

        public static Type GetPType(this OperandType enumValue)
        {
            Type type = null;
            MemberInfo memberInfo = enumValue.GetType().GetMember(enumValue.ToString())[0];
            foreach (var customAttribute in memberInfo.GetCustomAttributes(typeof(PaskellType), false))
            {
                type = ((PaskellType)customAttribute).Type;
            }

            return type;
        }
    }

    class RegexPattern : Attribute
    {
        public string Pattern;

        public RegexPattern(string pattern)
        {
            Pattern = pattern;
        }
    }

    class PaskellType : Attribute
    {
        public Type Type;

        public PaskellType(Type type)
        {
            Type = type;
        }
    }

    struct Token : IComparable
    {
        public readonly string Code;
        public readonly TokenType TokenType;
        public readonly int Index;

        public Token(string code, TokenType tokenType, int index)
        {
            Code = code;
            TokenType = tokenType;
            Index = index;
        }

        public int CompareTo(object obj)        //Necessary to be comparable by index so that the tokeniser can sort all tokens once made
        {
            Token token = (Token)obj;
            return Index - token.Index;
        }
    }

    public class TypeSignature
    {
        public bool IsFunction { get; }
        public Type Type { get; }                   // --Used if IsFunction is false
        public TypeSignature Parameter { get; }     // --Used if IsFunction is true
        public TypeSignature Return { get; }        // _/

        public dynamic Value => ToString(); //Mostly for debug purposes

        //Instantiates type signature of a variable, unspecified type
        public TypeSignature()
        {
            IsFunction = false;
        }

        //Instantiates type signature of a variable of type given
        public TypeSignature(Type type)
        {
            IsFunction = false;
            Type = type;
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
            return IsFunction ? $"{(brackets ? "(" : "")}{Parameter.ToString()} -> {Return.ToString(false)}{(brackets ? ")" : "")}" : (Type != null ? Type.ToString() : "_");
        }
    }

    public class PContext
    {
        public PExpression[] Expressions;

        public PContext(PExpression[] expressions)
        {
            Expressions = expressions;
        }
    }

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
        private readonly int argumentCount = 0;
        private readonly Stack<PExpression> arguments;
        private readonly Func<Stack<PExpression>, PExpression> function;

        //Instantiates expression representing data value
        public PExpression(dynamic value, string identifier)
        {
            Identifier = identifier;
            Value = value;
            type = PExpressionType.Evaluated;
            TypeSignature = new TypeSignature(value.GetType());
        }

        //Instatiates base function definition
        public PExpression(Func<Stack<PExpression>, PExpression> function, TypeSignature typeSignature, int argumentCount, string identifier = "")
        {
            Identifier = identifier;
            TypeSignature = typeSignature;
            arguments = new Stack<PExpression>();
            this.argumentCount = argumentCount;
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
        public PExpression(int parameterIndex)
        {
            isParamater = true;
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
                    (PExpression, int) workingExpressionTuple = SubExpressions.Pop();
                    PExpression workingExpression = workingExpressionTuple.Item1;
                    if (workingExpressionTuple.Item2 > 0)
                    {
                        for (int i = 0; i < workingExpressionTuple.Item2; i++)
                        {
                            workingExpression = workingExpression.Evaluate(tempStack.Pop());
                        }
                    }
                    tempStack.Push(workingExpression);
                }

                PExpression result = tempStack.Pop();

                if (!result.TypeSignature.IsFunction)
                {
                    result.type = PExpressionType.Evaluated;
                }
                else
                {
                    result.type = PExpressionType.Definition;
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
                if (type != PExpressionType.Definition)
                {
                    Stack<(PExpression, int)> tempStack = new Stack<(PExpression, int)>();
                    while (workedExpression.SubExpressions.Count > 0)
                    {
                        (PExpression, int) expression = workedExpression.SubExpressions.Pop();
                        if (expression.Item1.isParamater)
                        {
                            if (expression.Item1.parameterIndex == 0)
                            {
                                expression.Item1 = argument;
                            }
                            else
                            {
                                expression.Item1.parameterIndex--;
                            }
                        }
                        tempStack.Push(expression);
                    }
                    while (tempStack.Count > 0)
                    {
                        workedExpression.SubExpressions.Push(tempStack.Pop());
                    }
                    workedExpression.TypeSignature = workedExpression.TypeSignature.Return;
                }

                if (!workedExpression.TypeSignature.IsFunction)
                {
                    workedExpression = workedExpression.Evaluate();
                }

                return workedExpression;
            }
            else
            {
                workedExpression.arguments.Push(argument);
                workedExpression.TypeSignature = workedExpression.TypeSignature.Return;
                if (workedExpression.arguments.Count == workedExpression.argumentCount)
                {
                    Stack<PExpression> arguments = new Stack<PExpression>();
                    while (workedExpression.arguments.Count > 0)
                    {
                        arguments.Push(workedExpression.arguments.Pop());
                    }
                    PExpression result = function(arguments);
                    //if (!result.TypeSignature.IsFunction)
                    //{
                    //    result = result.Evaluate();
                    //}
                    workedExpression = result;
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
                    workedExpression = new PExpression(parameterIndex);
                }
                else if (isBaseExpression)
                {
                    workedExpression = new PExpression(function, TypeSignature, argumentCount, Identifier);
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

    public class ParserReturnState
    {
        public readonly bool Success;
        public readonly Stack<ParserReturnErrorInfo> Errors;

        public ParserReturnState()
        {
            Success = false;
            Errors = new Stack<ParserReturnErrorInfo>();
        }

        public ParserReturnState(bool success)
        {
            Success = success;
            Errors = new Stack<ParserReturnErrorInfo>();
        }
    }

    public struct ParserReturnErrorInfo
    {
        public ParserReturnError Error;
        public int Index;

        public ParserReturnErrorInfo(ParserReturnError error, int index)
        {
            Error = error;
            Index = index;
        }
    }

    public enum ParserReturnError
    {
        BadIdentifier,
        BadBracketNesting,
        MissingWordBeforeRelation
    }

    enum TokenType
    {
        [RegexPattern(@"\(|\)")]
        Bracket,
        [RegexPattern("=")]
        Equate,
        [RegexPattern("->")]
        FunctionMap,
        [RegexPattern(@"(?!true|false)((?<=(\s|\(|\)))|^)[A-Za-z][A-Za-z0-9]*")]
        Word,
        [RegexPattern(@"((?<=\s)|^)(([0-9]+(\.[0-9]+)?)|('.')|true|false)(?![A-Za-z0-9]|\.)")]
        Operand,
        [RegexPattern(@"(?<!\\)\n")]
        LineBreak
    }

    enum ParserState
    {
        TypeSignature,
        ExpressionSignature,
        Definition
    }

    public enum OperandType
    {
        [PaskellType(typeof(long))]
        Int,
        [PaskellType(typeof(double))]
        Float,
        [PaskellType(typeof(char))]
        Char,
        [PaskellType(typeof(bool))]
        Bool
    }

    enum PExpressionType
    {
        Definition,
        WorkedExpression,
        Evaluated
    }

    public class PaskellRuntimeException : Exception
    {
        public new PaskellRuntimeException InnerException;
        public PExpression PExpression;
        public string ErrorMessage;

        public PaskellRuntimeException(string errorMessage, PExpression pExpression, PaskellRuntimeException innerException = null)
        {
            ErrorMessage = errorMessage;
            PExpression = pExpression;
            InnerException = innerException;
        }
    }

    public class PaskellCompileException : Exception
    {
        public int Index;
        public string ErrorMessage;

        public PaskellCompileException(string errorMessage, int index)
        {
            ErrorMessage = errorMessage;
            Index = index;
        }
    }
}
