namespace Nebula.SyntaxNodes
{
    internal class ErrNode : StmtNode
    {
        public readonly string Lhs, ApiIdf;

        public ErrNode(string[] tokens) : base(tokens)
        {
            var parser = new Parse.Field(tokens, Tokens.TokenType.Err);
            Lhs = parser.Lhs;
            ApiIdf = parser.Obj;
        }
    }
}