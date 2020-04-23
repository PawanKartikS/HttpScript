namespace Nebula.SyntaxNodes
{
    internal class UrlNode : StmtNode
    {
        public string Endpoint;
        public readonly long Timeout;
        public readonly string Lhs, Method;

        public UrlNode(string[] tokens) : base(tokens)
        {
            var parser = new Parse.Url(tokens);
            Lhs = parser.Lhs;
            Method = parser.Method;
            Timeout = parser.Timeout;
            Endpoint = parser.Endpoint;
        }
    }
}