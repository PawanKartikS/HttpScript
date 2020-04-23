using System;
using System.Collections.Generic;

namespace Nebula.SyntaxNodes
{
    internal class FnCallNode : StmtNode
    {
        public readonly string FnName;
        public readonly List<Tuple<string, Tokens.TokenType>> FnArgs;

        public FnCallNode(string[] tokens) : base(tokens)
        {
            var parser = new Parse.FnCall(tokens);
            FnName = parser.FnName;
            FnArgs = parser.FnArgs;
        }
    }
}