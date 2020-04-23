namespace Nebula.SyntaxNodes
{
    internal class ReadNode : StmtNode
    {
        public readonly string Symbol;

        public ReadNode(string[] tokens) : base(tokens)
        {
            var parser = new Parse.Read(tokens);
            Symbol = parser.Symbol;
        }
    }
}