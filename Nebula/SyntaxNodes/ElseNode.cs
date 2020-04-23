namespace Nebula.SyntaxNodes
{
    internal class ElseNode : StmtNode
    {
        public readonly int TokensLength;
        public ElseNode(string[] tokens) : base(tokens)
        {
            TokensLength = tokens.Length;
        }
    }
}