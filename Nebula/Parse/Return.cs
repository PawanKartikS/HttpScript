using System;
using System.Collections.Generic;

namespace Nebula.Parse
{
    internal class Return : TokenStream
    {
        public readonly string Arg;
        public readonly Tokens.TokenType ArgType;

        public Return(IEnumerable<string> tokens) : base(tokens)
        {
            Ensure(Tokens.TokenType.Return, true);
            if (Empty())
                return;

            (Arg, ArgType) = Consume();
            _ = ArgType switch
            {
                Tokens.TokenType.StringLiteral => true,
                Tokens.TokenType.Variable      => true,
                Tokens.TokenType.Numeric       => true,
                _  => throw new ArgumentException("fatal: invalid argument for return")
            };

            Ensure(0);
        }
    }
}