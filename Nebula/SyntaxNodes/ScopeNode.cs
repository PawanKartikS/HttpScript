using System;

namespace Nebula.SyntaxNodes
{
    internal class ScopeNode : StmtNode
    {
        public ScopeNode(string[] tokens) : base(tokens)
        {
            if (tokens.Length > 1)
                throw new ArgumentException("fatal: excess tokens following scope token");
        }
    }
}