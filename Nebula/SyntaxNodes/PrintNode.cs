namespace Nebula.SyntaxNodes
{
    internal class PrintNode : StmtNode
    {
        public readonly string Str;
        public readonly Tokens.TokenType ArgType;

        public PrintNode(string[] tokens) : base(tokens)
        {
            var parser = new Parse.Print(tokens);
            Str = parser.Str;
            ArgType = parser.ArgType;
        }
    }
}