using System.Collections.Generic;

namespace Nebula.SyntaxNodes
{
    internal class DefNode : StmtNode
    {
        public readonly string FnName;
        public readonly List<string> FnArgs;

        public DefNode(string[] tokens) : base(tokens)
        {
            var parser = new Parse.Def(tokens);
            FnName = parser.FnName;
            FnArgs = parser.FnArgs;
        }
    }
}