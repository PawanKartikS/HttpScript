using System;
using System.Collections.Generic;

namespace Nebula.Parse
{
    internal class If : TokenStream
    {
        public enum IfType
        {
            LastSuccess,
            SimpleComparison,
            Unknown,
            VariableLookup,
        }
        
        public readonly IfType Type;
        public readonly string Lhs, Rhs;
        public readonly bool InverseEval;
        public readonly Tokens.TokenType LhsType, RhsType;
        public readonly Tokens.TokenType ComparisonOperator;
        
        public If(IEnumerable<string> tokens) : base(tokens)
        {
            InverseEval = false;
            Type = IfType.Unknown;

            Consume();
            if (Peek() == Tokens.TokenType.BooleanFalse)
            {
                InverseEval = true;
                Consume();
            }

            if (Peek() == Tokens.TokenType.Success)
            {
                Type = IfType.LastSuccess;
                Ensure(Tokens.TokenType.Success, true);
                Ensure(0);
                return;
            }

            (Lhs, LhsType) = Consume();
            if (Empty())
            {
                Type = IfType.VariableLookup;
                return;
            }

            var (comp, _) = Consume();
            if (Peek() == Tokens.TokenType.EqualTo)
            {
                var (t, _) = Consume();
                comp += t;
            }

            ComparisonOperator = Tokens.GetTokenType(comp);
            var isValid = ComparisonOperator switch
            {
                Tokens.TokenType.LessThan      => true,
                Tokens.TokenType.LessThanEq    => true,
                Tokens.TokenType.GreaterThan   => true,
                Tokens.TokenType.GreaterThanEq => true,
                Tokens.TokenType.CompEq        => true,
                Tokens.TokenType.NotEqual      => true,
                _ => false
            };

            if (!isValid)
                throw new ArgumentException($"fatal: invalid comparison operator {comp}");

            (Rhs, RhsType) = Consume();
            Ensure(0);
            if (Type == IfType.Unknown)
                Type = IfType.SimpleComparison;
        }
    }
}