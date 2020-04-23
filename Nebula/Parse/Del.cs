using System;
using System.Collections.Generic;

namespace Nebula.Parse
{
    internal class Del
    {
        public readonly string Var;

        public Del(IEnumerable<string> tokens)
        {
            var stream = new TokenStream(tokens);
            stream.Ensure(Tokens.TokenType.Del, true);
            
            if (stream.Peek() != Tokens.TokenType.Variable)
                throw new ArgumentException("parse: argument to del must be a variable");

            (Var, _)  = stream.Consume();
            stream.Ensure(0);
        }
    }
}