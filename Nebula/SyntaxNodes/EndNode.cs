using System;

namespace Nebula.SyntaxNodes
{
    internal class EndNode : StmtNode
    {
        public EndNode(string[] tokens) : base(tokens)
        {
            if (tokens.Length > 1)
                throw new ArgumentException("fatal: excess tokens");
        }
    }
}