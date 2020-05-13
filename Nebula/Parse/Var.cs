using System;
using System.Collections.Generic;

namespace Nebula.Parse
{
    internal class Var : TokenStream
    {
        public bool IsConstant;
        public string Lhs, Rhs;
        public Tokens.TokenType RhsType;

        public Var(IEnumerable<string> tokens) : base(tokens)
        {
        }

        public void ParseLhs()
        {
            Ensure(Tokens.TokenType.Var, true);
            if (Peek() == Tokens.TokenType.Const)
            {
                IsConstant = true;
                Ensure(Tokens.TokenType.Const, true);
            }

            if (Peek() != Tokens.TokenType.Variable)
                throw new ArgumentException("parse: lhs to var must be a variable");
            
            (Lhs, _) = Consume();
            Ensure(Tokens.TokenType.EqualTo, true);
        }

        public void ParseRhs()
        {
            (Rhs, RhsType) = Consume();
            _ = RhsType switch
            {
                Tokens.TokenType.StringLiteral => true,
                Tokens.TokenType.Variable => true,
                Tokens.TokenType.Numeric  => true,
                _  => throw new ArgumentException("fatal: specified value is not a valid type for rhs")
            };
            
            Ensure(0);
        }
    }
}