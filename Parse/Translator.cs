using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parse
{
    public static class Translator
    {
        public static SymbolicLong[,] Compile(string sourceCode)
        {
            ParserState state = ParserState.Global;

            foreach (var character )

            return;
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
