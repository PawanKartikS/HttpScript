using System;
using static Nebula.Tokens;

namespace Nebula
{
    internal static class Evaluator
    {
        public static bool Evaluate(string lhs, string rhs, TokenType lhsType, TokenType rhsType,
            TokenType comparisonOperator)
        {
            if (lhsType != rhsType)
                throw new ArgumentException("evaluator: cannot compare between 2 different types");

            if (comparisonOperator == TokenType.CompEq)
                return lhs.Equals(rhs);
            if (comparisonOperator == TokenType.NotEqual)
                return !lhs.Equals(rhs);

            if (lhsType == TokenType.StringLiteral)
            {
                if (comparisonOperator == TokenType.LessThan)
                    return string.Compare(lhs, rhs, StringComparison.Ordinal) < 0;
                if (comparisonOperator == TokenType.LessThanEq)
                    return string.Compare(lhs, rhs, StringComparison.Ordinal) <= 0;
                if (comparisonOperator == TokenType.GreaterThan)
                    return string.Compare(lhs, rhs, StringComparison.Ordinal) > 0;
                if (comparisonOperator == TokenType.GreaterThanEq)
                    return string.Compare(lhs, rhs, StringComparison.Ordinal) >= 0;
            }

            return comparisonOperator switch
            {
                TokenType.LessThan => (double.Parse(lhs) < double.Parse(rhs)),
                TokenType.LessThanEq => (double.Parse(lhs) <= double.Parse(rhs)),
                TokenType.GreaterThan => (double.Parse(lhs) > double.Parse(rhs)),
                TokenType.GreaterThanEq => (double.Parse(lhs) >= double.Parse(rhs)),
                _ => false
            };
        }
    }
}