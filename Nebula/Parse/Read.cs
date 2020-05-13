using System;
using System.Collections.Generic;

namespace Nebula.Parse
{
    internal class Read : TokenStream
    {
        public readonly string Symbol;

        public Read(IEnumerable<string> tokens) : base(tokens)
        {
            Ensure(Tokens.TokenType.Read, true);
            if (Peek() != Tokens.TokenType.Variable)
                throw new ArgumentException("parse: argument to read must be a variable");

            (Symbol, _) = Consume();
            Ensure(0);
        }
    }
}