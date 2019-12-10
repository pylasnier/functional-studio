﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace Parse
{
    public static class Translator
    {
        public static FunctionDefinition[] Compile(string sourceCode)
        {
            FunctionDefinition[] functions = new FunctionDefinition[1];
            functions[0] = new FunctionDefinition();
            List<SymbolicLong> code = new List<SymbolicLong>();

            for (int i = 0; i < sourceCode.Length; /*Increments handle within loop*/)
            {
                string functionName;
                List<string> parameters = new List<string>();

                ParserState state = ParserState.Global;
                while (String.IsNullOrWhiteSpace(sourceCode[i].ToString())) i++;

                while()
            }

            return;
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
