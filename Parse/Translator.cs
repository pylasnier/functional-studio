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
        private static ParserReturnState Tokenise(string sourceCode, out Token[] TokenCode)
        {
            List<Token> tokenCollection = new List<Token>();
            bool[] codeMatched = new bool[sourceCode.Length];
            ParserReturnState returnState = new ParserReturnState();
            MatchCollection matches;

            var OpenBracket = new Regex(@"\(");
            var CloseBracket = new Regex(@"\)");
            var Equate = new Regex("=");
            var FunctionMap = new Regex("->");
            var Word = new Regex("\b[A-Z|a-z][A-Z|a-z|0-9]*");
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
                tokenCollection.Add(new Token(match.Value, TokenType.Word, match.Index));   //Here the string depends on the source code, which could be any valid identifier
                codeMatched.Populate(true, match.Index, match.Length);
            }

            //Finding whitespace just to fill codeMatched, so that it doesn't detect spaces as syntax errors
            matches = new Regex(@"\s").Matches(sourceCode);
            foreach (Match match in matches)
            {
                codeMatched.Populate(true, match.Index, match.Length);
            }

            Token[] tokenString = Tools.
        }

        public static FunctionDefinition[] Compile(string sourceCode)
        {
            FunctionDefinition[] functions = new FunctionDefinition[1];
            functions[0] = new FunctionDefinition();
            List<SymbolicLong> code = new List<SymbolicLong>();

            for (int i = 0; i < sourceCode.Length; /*Increments handled within loop*/)
            {
                string functionName;
                List<string> parameters = new List<string>();

                ParserState state = ParserState.Global;
                while (String.IsNullOrWhiteSpace(sourceCode[i].ToString())) i++;

                while()
            }

            return;
        }

        private struct Token : IComparable
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
                OperandType = OperandType.Integer;
                ArrayLength = 0;
                IsArrayElement = false;
            }

            public static implicit operator ulong(SymbolicLong s) => s.CodeLong;
            public static explicit operator SymbolicLong(ulong u) => new SymbolicLong(u);
        }

        public struct ParserReturnState
        {
            public bool Success;
            public Stack<ParserReturnErrorInfo> Errors;
        }

        public struct ParserReturnErrorInfo
        {
            ParserReturnError Error;
            int Index;
        }

        public enum ParserReturnError
        {
            InvalidSyntax,
            BadBracketNesting,
            MissingWordBeforeRelation
        }

        private enum TokenType
        {
            OpenBracket,
            CloseBracket,
            Equate,
            FunctionMap,
            Word,
            Semicolon
        }

        private enum ParserState
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
    }
}
