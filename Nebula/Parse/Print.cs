using System;
using System.Collections.Generic;

namespace Nebula.Parse
{
    internal class Print : TokenStream
    {
        public readonly string Str;
        public readonly Tokens.TokenType ArgType;

        public Print(IEnumerable<string> tokens) : base(tokens)
        {
            Ensure(Tokens.TokenType.Print, true);
            (Str, ArgType) = Consume();
            _ = ArgType switch
            {
                Tokens.TokenType.StringLiteral => true,
                Tokens.TokenType.Variable      => true,
                Tokens.TokenType.Numeric       => true,
                _  => throw new ArgumentException("fatal: specified value is not a valid type for rhs")
            };
            
            Ensure(0);
        }
    }
}