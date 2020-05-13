using System;
using System.Linq;
using System.Collections.Generic;

namespace Nebula
{
    internal class TokenStream
    {
        private readonly Queue<string> _stream;

        protected TokenStream(IEnumerable<string> tokens)
        {
            _stream = new Queue<string>();
            foreach (var token in tokens)
                _stream.Enqueue(token);
        }

        public Tuple<string, Tokens.TokenType> Consume()
        {
            if (Empty())
                throw new ArgumentException("fatal: stream is empty");

            var token = _stream.Dequeue();
            var type = Tokens.GetTokenType(token);

            return type != Tokens.TokenType.StringLiteral
                ? new Tuple<string, Tokens.TokenType>(token, type)
                : new Tuple<string, Tokens.TokenType>(token.Substring(1, token.Length - 2),
                    Tokens.TokenType.StringLiteral);
        }

        protected bool Empty() => _stream.Count == 0;

        public void Ensure(Tokens.TokenType type, bool discard = false, bool strictCheck = true)
        {
            if (_stream.Count == 0)
            {
                if (!strictCheck)
                    return;

                throw new ArgumentException($"fatal: stream is empty");
            }

            var front = _stream.Peek();
            if (Tokens.GetTokenType(front) != type && strictCheck)
                throw new ArgumentException($"fatal: expecting token of type {type}");

            if (discard)
                _stream.Dequeue();
        }

        protected void Ensure(int size)
        {
            if (size == 0 && !Empty() && Tokens.GetTokenType(_stream.Peek()) == Tokens.TokenType.Comment)
                return;

            if (_stream.Count != size)
                throw new ArgumentException(
                    $"fatal: found {(_stream.Count > size ? "more" : "less")} tokens than required");
        }

        public Tokens.TokenType LookAhead()
        {
            return _stream.Count > 1 ? Tokens.GetTokenType(_stream.ElementAt(1)) : Tokens.TokenType.Unknown;
        }

        public Tokens.TokenType Peek()
        {
            if (Empty())
                throw new ArgumentException("fatal: expecting more tokens");
            return Tokens.GetTokenType(_stream.Peek());
        }
    }
}