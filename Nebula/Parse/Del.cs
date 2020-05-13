using System;
using System.Collections.Generic;

namespace Nebula.Parse
{
    internal class Del : TokenStream
    {
        public readonly string Var;

        public Del(IEnumerable<string> tokens) : base(tokens)
        {
            Ensure(Tokens.TokenType.Del, true);
            if (Peek() != Tokens.TokenType.Variable)
                throw new ArgumentException("parse: argument to del must be a variable");

            (Var, _)  = Consume();
            Ensure(0);
        }
    }
}