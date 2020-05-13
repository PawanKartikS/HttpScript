using System;
using System.Collections.Generic;

namespace Nebula.Parse
{
    internal class Url : TokenStream
    {
        public long Timeout = 5000;
        public readonly string Lhs;
        public bool TimeoutBehaviour = false;
        public string Endpoint, Method = "GET";

        private void TryParseComma()
        {
            var advance = LookAhead();
            if (advance == Tokens.TokenType.Unknown || advance == Tokens.TokenType.CloseBr)
                return;
            Ensure(Tokens.TokenType.Comma, true);
        }

        public Url(IEnumerable<string> tokens) : base(tokens)
        {
            Ensure(Tokens.TokenType.Url, true);
            if (Peek() != Tokens.TokenType.Variable)
                throw new ArgumentException("parse: argument to read must be a variable");

            (Lhs, _) = Consume();
            Ensure(Tokens.TokenType.EqualTo, true);
            ParseParams();
        }

        private void ParseParams()
        {
            var parsedParams = false;
            Ensure(Tokens.TokenType.OpenBr, true);
            while (Peek() != Tokens.TokenType.CloseBr)
            {
                parsedParams = true;
                if (Peek() == Tokens.TokenType.Endpoint)
                    ParseEndpoint();
                else if (Peek() == Tokens.TokenType.Halt)
                    ParseTimeoutBehaviour();
                else if (Peek() == Tokens.TokenType.Method)
                    ParseMethod();
                else if (Peek() == Tokens.TokenType.Timeout)
                    ParseTimeout();
                else
                    throw new ArgumentException($"fatal: expecting parameter instead of {Consume()}");
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
            
            Ensure(Tokens.TokenType.CloseBr, true);
            Ensure(0);
        }

        private void ParseEndpoint()
        {
            Ensure(Tokens.TokenType.Endpoint, true);
            Ensure(Tokens.TokenType.FieldAccess, true);

            (Endpoint, _) = Consume();
            TryParseComma();
        }

        private void ParseMethod()
        {
            Ensure(Tokens.TokenType.Method, true);
            Ensure(Tokens.TokenType.FieldAccess, true);
            
            (Method, _) = Consume();
            TryParseComma();
        }

        private void ParseTimeout()
        {
            Ensure(Tokens.TokenType.Timeout, true);
            Ensure(Tokens.TokenType.FieldAccess, true);
            
            var (timeout, _) = Consume();
            Timeout = long.Parse(timeout);
            TryParseComma();
        }

        private void ParseTimeoutBehaviour()
        {
            Ensure(Tokens.TokenType.Halt, true);
            Ensure(Tokens.TokenType.FieldAccess, true);

            var (timeoutBehaviour, _) = Consume();
            TimeoutBehaviour = bool.Parse(timeoutBehaviour);
            TryParseComma();
        }
    }
}