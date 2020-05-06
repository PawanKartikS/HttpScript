using System;
using static Nebula.Tokens;
using System.Collections.Generic;

namespace Nebula.Parse
{
    internal class Unary
    {
        public readonly string Arg;

        public Unary(IEnumerable<string> tokens)
        {
            var stream = new TokenStream(tokens);
            (Arg, _) = stream.Consume();
            Arg = Arg.Substring(0, Arg.Length - 2);
            
            if (GetTokenType(Arg) != TokenType.Variable)
                throw new ArgumentException("parse: Unary() arg must be a variable");
        }
    }
}