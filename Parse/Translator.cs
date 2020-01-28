using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using Utility;

namespace Parse
{
    public static class Translator
    {
        public static void CallMe(string code)
        {
            Token[] output;
            Tokenise(code, out output);
        }

        private static ParserReturnState Tokenise(string sourceCode, out Token[] TokenCode)
        {
            List<Token> tokenCollection = new List<Token>();
            bool[] codeMatched = new bool[sourceCode.Length];
            ParserReturnState returnState;
            MatchCollection matches;

            var OpenBracket = new Regex(@"\(");
            var CloseBracket = new Regex(@"\)");
            var Equate = new Regex("=");
            var FunctionMap = new Regex("->");
            var Word = new Regex(@"(?<=\s)[A-Z|a-z][A-Z|a-z|0-9]*|^[A-Z|a-z][A-Z|a-z|0-9]*");
            var Operand = new Regex(@"(?<=\s)" + "[[0-9]+[\\.[0-9]+]?|\'.\'|true|false" + "^[[0-9]+[\\.[0-9]+]?|\'.\'|true|false");
            var Semicolon = new Regex(";");         //Don't implement this yet, will be used for sequential code later

            codeMatched.Populate(false, 0, codeMatched.Length);

            //OpenBracket matching
            matches = OpenBracket.Matches(sourceCode);
            foreach (Match match in matches)
            {
                tokenCollection.Add(new Token("(", TokenType.OpenBracket, match.Index));        //Passing the string for the operator here is actually redundant; the translator is
                codeMatched.Populate(true, match.Index, match.Length);                          //capable of identifying what the token represents by its type alone. Just for good measure
            }

            //CloseBracket matching
            matches = CloseBracket.Matches(sourceCode);
            foreach (Match match in matches)
            {
                tokenCollection.Add(new Token(")", TokenType.CloseBracket, match.Index));
                codeMatched.Populate(true, match.Index, match.Length);
            }

            //Equate matching
            matches = Equate.Matches(sourceCode);
            foreach (Match match in matches)
            {
                tokenCollection.Add(new Token("=", TokenType.Equate, match.Index));
                codeMatched.Populate(true, match.Index, match.Length);
            }

            //FunctionMap matching
            matches = FunctionMap.Matches(sourceCode);
            foreach (Match match in matches)
            {
                tokenCollection.Add(new Token("->", TokenType.FunctionMap, match.Index));
                codeMatched.Populate(true, match.Index, match.Length);
            }

            //Word matching
            matches = Word.Matches(sourceCode);
            foreach (Match match in matches)
            {
                if (match.Value != "true" && match.Value != "false")        //Only operands that could be interpreted as words, but shouldn't be
                {
                    tokenCollection.Add(new Token(match.Value, TokenType.Word, match.Index));   //Here the string depends on the source code, which could be any valid identifier
                    codeMatched.Populate(true, match.Index, match.Length);
                }
            }

            //Operand matching
            matches = Operand.Matches(sourceCode);
            foreach (Match match in matches)
            {
                tokenCollection.Add(new Token(match.Value, TokenType.Operand, match.Index));
                codeMatched.Populate(true, match.Index, match.Length);
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
            if (!codeMatched.All(element => element == true))
            {
                returnState = new ParserReturnState(false);
                //Loops through to find each occurence of an error
                for (int i = 0; i < codeMatched.Length; /*Increments handled in loop*/)
                {
                    if (codeMatched[i] == false)
                    {
                        returnState.Errors.Push(new ParserReturnErrorInfo(ParserReturnError.InvalidSyntax, i));
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

    struct Token : IComparable
    {
        string Word;
        TokenType TokenType;
        int Index;

        public Token(string word, TokenType tokenType, int index)
        {
            Word = word;
            TokenType = tokenType;
            Index = index;
        }

        public int CompareTo(object obj)        //Necessary to be comparable by index so that the tokeniser can sort all tokens once made
        {
            Token token = (Token)obj;
            return Index - token.Index;
        }
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
            OperandType = OperandType.Integer;
            ArrayLength = 0;
            IsArrayElement = false;
        }

        public static implicit operator ulong(SymbolicLong s) => s.CodeLong;
        public static explicit operator SymbolicLong(ulong u) => new SymbolicLong(u);
    }

    public class PFunction
    {
        IType ArgumentType;

        public PFunction Evaluate()
        {
            throw new NotImplementedException();
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
        ParserReturnError Error;
        int Index;

        public ParserReturnErrorInfo(ParserReturnError error, int index)
        {
            Error = error;
            Index = index;
        }
    }

    public enum ParserReturnError
    {
        InvalidSyntax,
        BadBracketNesting,
        MissingWordBeforeRelation
    }

    enum TokenType
    {
        OpenBracket,
        CloseBracket,
        Equate,
        FunctionMap,
        Word,
        Operand,
        Semicolon
    }

    enum ParserState
    {
        Global,
        Function
    }

    public enum OperandType
    {
        Integer,
        Float,
        Character,
        Boolean,
        Array
    }

    public interface IType
    {
        Type Type();
    }

    public struct PInteger : IType
    {
        public Type Type() => typeof(int);
    }

    public struct PFloat : IType
    {
        public Type Type() => typeof(float);
    }

    public struct PCharacter : IType
    {
        public Type Type() => typeof(char);
    }

    public struct PBool : IType
    {
        public Type Type() => typeof(bool);
    }

    public struct PArray<T> : IType where T : IType
    {
        public Type Type() => typeof(T[]);
    }

    public 

    public class PaskellRuntimeException : Exception
    {
        new public PaskellRuntimeException InnerException;
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
