using System;
using System.Collections.Generic;

namespace Nebula.Parse
{
    internal class If
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
        
        public If(IEnumerable<string> tokens)
        {
            InverseEval = false;
            Type = IfType.Unknown;

            var stream = new TokenStream(tokens);
            stream.Consume();
            
            if (stream.Peek() == Tokens.TokenType.BooleanFalse)
            {
                InverseEval = true;
                stream.Consume();
            }

            if (stream.Peek() == Tokens.TokenType.Success)
            {
                Type = IfType.LastSuccess;
                stream.Ensure(Tokens.TokenType.Success, true);
                stream.Ensure(0);
                return;
            }

            (Lhs, LhsType) = stream.Consume();
            if (stream.Empty())
            {
                Type = IfType.VariableLookup;
                return;
            }

            var (comp, _) = stream.Consume();
            if (stream.Peek() == Tokens.TokenType.EqualTo)
            {
                var (t, _) = stream.Consume();
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

            (Rhs, RhsType) = stream.Consume();
            stream.Ensure(0);
            if (Type == IfType.Unknown)
                Type = IfType.SimpleComparison;
        }
    }
}