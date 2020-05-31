using static Nebula.Tokens;
using System.Collections.Generic;

namespace Nebula.SyntaxNodes
{
    internal class DefNode : StmtNode
    {
        public readonly string FnName;
        public readonly TokenType ReturnType;
        public readonly List<string> FnArgs;

        public DefNode(string[] tokens) : base(tokens)
        {
            var parser = new Parse.Def(tokens);
            FnName = parser.FnName;
            ReturnType = parser.ReturnType;
            FnArgs = parser.FnArgs;
        }
    }
}