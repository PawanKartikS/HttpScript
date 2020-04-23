using System;
using System.Collections.Generic;

namespace Nebula.Parse
{
    internal class Url
    {
        public long Timeout = 5000;
        public readonly string Lhs;
        public bool TimeoutBehaviour = false;
        public string Endpoint, Method = "GET";

        private readonly Func<TokenStream, bool> _tryParseComma = (stream) =>
        {
            var advance = stream.LookAhead();
            if (advance == Tokens.TokenType.Unknown || advance == Tokens.TokenType.CloseBr)
                return false;
            stream.Ensure(Tokens.TokenType.Comma, true);
            return true;
        };

        public Url(IEnumerable<string> tokens)
        {
            var stream = new TokenStream(tokens);
            stream.Ensure(Tokens.TokenType.Url, true);
            
            if (stream.Peek() != Tokens.TokenType.Variable)
                throw new ArgumentException("parse: argument to read must be a variable");

            (Lhs, _) = stream.Consume();
            stream.Ensure(Tokens.TokenType.EqualTo, true);
            ParseParams(stream);
        }

        private void ParseParams(TokenStream stream)
        {
            var parsedParams = false;
            stream.Ensure(Tokens.TokenType.OpenBr, true);
            while (stream.Peek() != Tokens.TokenType.CloseBr)
            {
                parsedParams = true;
                if (stream.Peek() == Tokens.TokenType.Endpoint)
                    ParseEndpoint(stream);
                else if (stream.Peek() == Tokens.TokenType.Halt)
                    ParseTimeoutBehaviour(stream);
                else if (stream.Peek() == Tokens.TokenType.Method)
                    ParseMethod(stream);
                else if (stream.Peek() == Tokens.TokenType.Timeout)
                    ParseTimeout(stream);
                else
                    throw new ArgumentException($"fatal: expecting parameter instead of {stream.Consume()}");
            }
            
            if (!parsedParams)
                throw new ArgumentException($"fatal: could not parse parameters for API {Lhs}");

            var isMethodValid = Method switch
            {
                "get" => true,
                "GET" => true,
                _ => false
            };
            
            if (!isMethodValid)
                throw new ArgumentException($"fatal: invalid method specified: {Method}");
            
            stream.Ensure(Tokens.TokenType.CloseBr, true);
            stream.Ensure(0);
        }

        private void ParseEndpoint(TokenStream stream)
        {
            stream.Ensure(Tokens.TokenType.Endpoint, true);
            stream.Ensure(Tokens.TokenType.FieldAccess, true);

            (Endpoint, _) = stream.Consume();
            _tryParseComma(stream);
        }

        private void ParseMethod(TokenStream stream)
        {
            stream.Ensure(Tokens.TokenType.Method, true);
            stream.Ensure(Tokens.TokenType.FieldAccess, true);
            
            (Method, _) = stream.Consume();
            _tryParseComma(stream);
        }

        private void ParseTimeout(TokenStream stream)
        {
            stream.Ensure(Tokens.TokenType.Timeout, true);
            stream.Ensure(Tokens.TokenType.FieldAccess, true);
            
            var (timeout, _) = stream.Consume();
            Timeout = long.Parse(timeout);
            _tryParseComma(stream);
        }

        private void ParseTimeoutBehaviour(TokenStream stream)
        {
            stream.Ensure(Tokens.TokenType.Halt, true);
            stream.Ensure(Tokens.TokenType.FieldAccess, true);

            var (timeoutBehaviour, _) = stream.Consume();
            TimeoutBehaviour = bool.Parse(timeoutBehaviour);
            _tryParseComma(stream);
        }
    }
}