namespace Nebula.SyntaxNodes
{
    internal class UseNode : StmtNode
    {
        public readonly Tokens.TokenType Arg;

        public UseNode(string[] tokens) : base(tokens)
        {
            var parser = new Parse.Use(tokens);
            Arg = parser.Arg;
        }
    }
}