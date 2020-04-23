namespace Nebula.SyntaxNodes
{
    internal class ResNode : StmtNode
    {
        public readonly string Lhs, ApiIdf;

        public ResNode(string[] tokens) : base(tokens)
        {
            var parser = new Parse.Field(tokens, Tokens.TokenType.Res);
            Lhs = parser.Lhs;
            ApiIdf = parser.Obj;
        }
    }
}