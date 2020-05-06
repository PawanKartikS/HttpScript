using System;

namespace Nebula.SyntaxNodes
{
    internal class UnaryNode : StmtNode
    {
        public readonly string Arg;

        public UnaryNode(string[] tokens) : base(tokens)
        {
            if (tokens.Length > 3)
                throw new ArgumentException("fatal: excess tokens for UnaryNode");

            var parser = new Parse.Unary(tokens);
            Arg = parser.Arg;
        }
    }
}