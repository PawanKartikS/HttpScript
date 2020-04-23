namespace Nebula.SyntaxNodes
{
    internal class IfNode : StmtNode
    {
        public readonly string Lhs, Rhs;
        public readonly bool InverseEval;
        public readonly Parse.If.IfType IfType;
        public readonly Tokens.TokenType ComparisonOperator;
        public readonly Tokens.TokenType LhsType, RhsType;

        public IfNode(string[] tokens) : base(tokens)
        {
            var parser = new Parse.If(tokens);
            Lhs = parser.Lhs;
            Rhs = parser.Rhs;
            IfType = parser.Type;
            LhsType = parser.LhsType;
            RhsType = parser.RhsType;
            InverseEval = parser.InverseEval;
            ComparisonOperator = parser.ComparisonOperator;
        }
    }
}