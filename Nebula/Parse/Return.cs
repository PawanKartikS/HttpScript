using System;
using System.Collections.Generic;

namespace Nebula.Parse
{
    internal class Return
    {
        public readonly string Arg;
        public readonly Tokens.TokenType ArgType;

        public Return(IEnumerable<string> tokens)
        {
            var stream = new TokenStream(tokens);
            stream.Ensure(Tokens.TokenType.Return, true);

            if (stream.Empty())
                return;

            (Arg, ArgType) = stream.Consume();
            _ = ArgType switch
            {
                Tokens.TokenType.StringLiteral => true,
                Tokens.TokenType.Variable      => true,
                Tokens.TokenType.Numeric       => true,
                _  => throw new ArgumentException("fatal: invalid argument for return")
            };

            stream.Ensure(0);
        }
    }
}