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

        private enum ParserState
        {
            Global,
            Function
        }

        public struct SymbolicLong
        {
            public ulong CodeLong { get; private set; }

            public SymbolicLong(ulong uulong)
            {
                CodeLong = uulong;
            }

            public static implicit operator ulong(SymbolicLong s) => s.CodeLong;
            public static explicit operator SymbolicLong(ulong u) => new SymbolicLong(u);
        }
    }
}
