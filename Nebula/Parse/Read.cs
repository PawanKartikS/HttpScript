using System;
using System.Collections.Generic;

namespace Nebula.Parse
{
    internal class Read
    {
        public readonly string Symbol;

        public Read(IEnumerable<string> tokens)
        {
            var stream = new TokenStream(tokens);
            stream.Ensure(Tokens.TokenType.Read, true);
            
            if (stream.Peek() != Tokens.TokenType.Variable)
                throw new ArgumentException("parse: argument to read must be a variable");

            (Symbol, _) = stream.Consume();
            stream.Ensure(0);
        }
    }
}