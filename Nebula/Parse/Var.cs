using System;
using System.Collections.Generic;

namespace Nebula.Parse
{
    internal class Var
    {
        public bool IsConstant;
        public string Lhs, Rhs;
        public Tokens.TokenType RhsType;
        private readonly TokenStream _stream;

        public Var(IEnumerable<string> tokens)
        {
            _stream = new TokenStream(tokens);
        }

        public void ParseLhs()
        {
            _stream.Ensure(Tokens.TokenType.Var, true);
            if (_stream.Peek() == Tokens.TokenType.Const)
            {
                IsConstant = true;
                _stream.Ensure(Tokens.TokenType.Const, true);
            }

            if (_stream.Peek() != Tokens.TokenType.Variable)
                throw new ArgumentException("parse: lhs to var must be a variable");
            
            (Lhs, _) = _stream.Consume();
            _stream.Ensure(Tokens.TokenType.EqualTo, true);
        }

        public void ParseRhs()
        {
            (Rhs, RhsType) = _stream.Consume();
            _ = RhsType switch
            {
                Tokens.TokenType.StringLiteral => true,
                Tokens.TokenType.Variable => true,
                Tokens.TokenType.Numeric  => true,
                _  => throw new ArgumentException("fatal: specified value is not a valid type for rhs")
            };
            
            _stream.Ensure(0);
        }
    }
}