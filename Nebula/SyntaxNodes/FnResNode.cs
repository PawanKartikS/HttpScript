using System;
using System.Linq;
using System.Collections.Generic;

namespace Nebula.SyntaxNodes
{
    internal class FnResNode : StmtNode
    {
        public readonly string Lhs, FnName;
        public readonly List<Tuple<string, Tokens.TokenType>> FnArgs;
        
        public FnResNode(string[] tokens) : base(tokens)
        {
            var lhsParser = new Parse.Var(tokens);
            var rhsParser = new Parse.FnCall(tokens.Skip(3));
            
            lhsParser.ParseLhs();
            Lhs = lhsParser.Lhs;
            FnName = rhsParser.FnName;
            FnArgs = rhsParser.FnArgs;
        }
    }
}