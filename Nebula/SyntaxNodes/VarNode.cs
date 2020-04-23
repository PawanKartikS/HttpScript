namespace Nebula.SyntaxNodes
{
    internal class VarNode : StmtNode
    {
        public readonly string Lhs, Rhs;
        public readonly bool IsConstant;
        public readonly Tokens.TokenType RhsType;

        public VarNode(string[] tokens) : base(tokens)
        {
            var parser = new Parse.Var(tokens);
            parser.ParseLhs();
            parser.ParseRhs();
            
            Lhs = parser.Lhs;
            Rhs = parser.Rhs;
            RhsType = parser.RhsType;
            IsConstant = parser.IsConstant;
        }
    }
}