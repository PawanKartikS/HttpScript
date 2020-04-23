using System.Collections.Generic;

namespace Nebula.Parse
{
    internal class Use
    {
        public readonly Tokens.TokenType Arg;

        public Use(IEnumerable<string> tokens)
        {
            var stream = new TokenStream(tokens);
            stream.Ensure(Tokens.TokenType.Use, true);

            Arg = stream.Peek();
            stream.Consume();
            stream.Ensure(0);
        }
    }
}