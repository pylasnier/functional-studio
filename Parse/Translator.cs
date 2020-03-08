﻿using System;
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
            return Tokenise(code, out output);
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
            List<PExpression> myExpressions = new List<PExpression>();
            bool[] codeMatched = new bool[tokenCode.Length];
            ParserReturnState returnState;

            codeMatched.Populate(false, 0, tokenCode.Length);

            for (int i = 0; i < tokenCode.Length; /*Increment handled inside of loop*/)
            {
                PExpression myExpression = new PExpression();
                bool isFunction = false;
                Type type = null;

                if (tokenCode[i].TokenType == TokenType.Word)
                {
                    foreach (OperandType operandType in Enum.GetValues(typeof(OperandType)))
                    {
                        if (tokenCode[i].Code == operandType.ToString())
                        {
                            type = operandType.GetPType();
                            codeMatched[i] = true;
                            i++;
                            break;
                        }
                    }
                }

                if (type != null)
                {
                    if (tokenCode[i].TokenType == TokenType.Word)
                    {
                        myExpression.Identifier = tokenCode[i].Code;
                        i++;

                        if (tokenCode[i].TokenType == TokenType.Equate)
                        {
                            i++;

                            if (tokenCode[i].TokenType == TokenType.Operand)
                            {
                                var converter = TypeDescriptor.GetConverter(type);
                                try
                                {
                                    myExpression.Value = converter.ConvertFrom(tokenCode[i].Code);
                                    myExpressions.Add(myExpression);
                                    codeMatched[i] = true;
                                }
                                catch { }
                            }
                        }
                    }
                }

                i++;
            }

            Context = new PContext(myExpressions.ToArray());

            if (codeMatched.Any(element => element == false))
            {
                returnState = new ParserReturnState(false);
                for (int i = 0; i < codeMatched.Length; i++)
                {
                    if (codeMatched[i] == false)
                    {
                        returnState.Errors.Push(new ParserReturnErrorInfo(ParserReturnError.BadIdentifier, i));
                    }
                }
            }
            else
            {
                returnState = new ParserReturnState(true);
            }

            return returnState;
        }

        //public static FunctionDefinition[] Compile(string sourceCode)
        //{
        //    FunctionDefinition[] functions = new FunctionDefinition[1];
        //    functions[0] = new FunctionDefinition();
        //    List<SymbolicLong> code = new List<SymbolicLong>();

        //    for (int i = 0; i < sourceCode.Length; /*Increments handled within loop*/)
        //    {
        //        string functionName;
        //        List<string> parameters = new List<string>();

        //        ParserState state = ParserState.Global;
        //        while (String.IsNullOrWhiteSpace(sourceCode[i].ToString())) i++;

        //        while()
        //    }

        //    return;
        //}
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
        public bool IsFunction;
        public Type Type;            // --Used if IsFunction is false
        public TypeSignature Parameter;     // --Used if IsFunction is true
        public TypeSignature Return;        // _/
    }

    public struct FunctionDefinition
    {
        public ulong CallHash;
        public OperandType InputType;
        public OperandType OutputType;
        public SymbolicLong[] SymbolicCode;
    }

    public struct SymbolicLong
    {
        public ulong CodeLong { get; private set; } //Custom getters and setters need to be written, both directly changing other SymbolicLong properties representing the code

        public bool IsFunctionCall;
        public OperandType OperandType;
        public int ArrayLength;
        public bool IsArrayElement;

        public SymbolicLong(ulong uulong)
        {
            CodeLong = uulong;

            //Gonna have to remove these later
            IsFunctionCall = false;
            OperandType = OperandType.Int;
            ArrayLength = 0;
            IsArrayElement = false;
        }

        public static implicit operator ulong(SymbolicLong s) => s.CodeLong;
        public static explicit operator SymbolicLong(ulong u) => new SymbolicLong(u);
    }

    public class PContext
    {
        public PExpression[] Expressions;

        public PContext(PExpression[] expressions)
        {
            Expressions = expressions;
        }
    }

    public class PFunction
    {
        public OperandType ArgumentType;

        public PFunction Evaluate(object arg)
        {
            throw new NotImplementedException();
        }
    }

    public class PExpression
    {
        public TypeSignature TypeSignature;
        public string Identifier;
        public dynamic Value;
        public Queue<PExpression> SubExpressions = new Queue<PExpression>();
        public bool IsArgument;
        public int ArgumentIndex;
        public bool Evaluated;

        public bool IsBaseExpression;
        public int ArgumentCount = 0;
        public Queue<PExpression> Arguments = new Queue<PExpression>();
        public Func<Queue<dynamic>, dynamic> Function;

        public PExpression()
        {
            TypeSignature = new TypeSignature { IsFunction = false };
        }

        public PExpression(string identifier)
        {
            TypeSignature = new TypeSignature { IsFunction = false };
            Identifier = identifier;
        }

        public PExpression(string identifier, dynamic value)
        {
            TypeSignature = new TypeSignature { IsFunction = false };
            Identifier = identifier;
            Value = value;
        }

        public PExpression Evaluate()
        {
            if (Evaluated)
            {
                return this;
            }
            else
            {
                PExpression workingExpression = SubExpressions.Dequeue();
                while (SubExpressions.Count > 0)
                {
                    workingExpression = workingExpression.Evaluate(SubExpressions.Dequeue());
                }

                workingExpression.Evaluated = true;
                return workingExpression;
            }
        }

        public PExpression Evaluate(PExpression argument)
        {
            if (!IsBaseExpression)
            {
                for (int i = 0; i < SubExpressions.Count; i++)
                {
                    PExpression expression = SubExpressions.Dequeue();
                    if (expression.IsArgument)
                    {
                        if (expression.ArgumentIndex == 0)
                        {
                            expression = argument;
                        }
                        else
                        {
                            expression.ArgumentIndex--;
                        }
                    }
                    SubExpressions.Enqueue(expression);
                }

                TypeSignature.IsFunction = TypeSignature.Return.IsFunction;
                if (TypeSignature.IsFunction)
                {
                    TypeSignature.Parameter = TypeSignature.Return.Parameter;
                    TypeSignature.Return = TypeSignature.Return.Return;
                }
                else
                {
                    TypeSignature.Type = TypeSignature.Return.Type;
                    PExpression returnValue = Evaluate();
                    return returnValue;
                }

                return this;
            }
            else
            {
                PExpression returnValue;
                Arguments.Enqueue(argument);
                if (Arguments.Count == ArgumentCount)
                {
                    Queue<dynamic> evaluatedArgs = new Queue<dynamic>();
                    foreach (var expression in Arguments)
                    {
                        evaluatedArgs.Enqueue(expression.Evaluate().Value);
                    }
                    var result = Function(evaluatedArgs);
                    returnValue = new PExpression();
                    returnValue.Value = result;
                    returnValue.Evaluated = true;
                    returnValue.IsBaseExpression = false;
                }
                else
                {
                    returnValue = this;
                }

                return returnValue;
            }
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
        [RegexPattern(@"(?!true|false)((?<=\s)|^)[A-Za-z][A-Za-z0-9]*")]
        Word,
        [RegexPattern(@"((?<=\s)|^)(([0-9]+(\.[0-9]+)?)|('.')|true|false)(?![A-Za-z0-9]|\.)")]
        Operand,
        [RegexPattern(";")]
        Semicolon
    }

    enum ParserState
    {
        Declaration,
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

    public class PaskellRuntimeException : Exception
    {
        public new PaskellRuntimeException InnerException;
        public PFunction PFunction;
        public string ErrorMessage;

        public PaskellRuntimeException(string errorMessage, PFunction pFunction, PaskellRuntimeException innerException = null)
        {
            ErrorMessage = errorMessage;
            PFunction = pFunction;
            InnerException = innerException;
        }
    }
}
