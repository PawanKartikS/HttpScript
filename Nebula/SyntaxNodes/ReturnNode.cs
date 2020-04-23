namespace Nebula.SyntaxNodes
{
    internal class ReturnNode : StmtNode
    {
        public readonly string Arg;
        public readonly Tokens.TokenType ArgType;

        public ReturnNode(string[] tokens) : base(tokens)
        {
            var parser = new Parse.Return(tokens);
            Arg = parser.Arg;
            ArgType = parser.ArgType;
        }
    }
}