using System;
using static Nebula.Tokens;
using System.Collections.Generic;

namespace Nebula.Parse
{
    internal class Indexer : TokenStream
    {
        public readonly string Identifier;
        public readonly int IndexValue;
        public Indexer(IEnumerable<string> tokens) : base(tokens)
        {
            if (Peek() != TokenType.Variable)
                throw new ArgumentException("parse: invalid identifier for indexer");
            
            (Identifier, _) = Consume();
            
            Ensure(TokenType.SqBrOpen, true);
            if (Peek() != TokenType.Numeric)
                throw new ArgumentException("parse: invalid index value for indexer");

            IndexValue = int.Parse(Consume().Item1);
            Ensure(TokenType.SqBrClose, true);
        }
    }
}