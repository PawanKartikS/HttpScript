using System.Collections.Generic;

namespace Nebula.Parse
{
    internal class Use : TokenStream
    {
        public readonly Tokens.TokenType Arg;

        public Use(IEnumerable<string> tokens) : base(tokens)
        {
            Ensure(Tokens.TokenType.Use, true);
            Arg = Peek();
            Consume();
            Ensure(0);
        }
    }
}