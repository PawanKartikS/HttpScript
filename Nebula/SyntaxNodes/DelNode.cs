namespace Nebula.SyntaxNodes
{
    internal class DelNode : StmtNode
    {
        public readonly string Var;

        public DelNode(string[] tokens) : base(tokens)
        {
            var parser = new Parse.Del(tokens);
            Var = parser.Var;
        }
    }
}