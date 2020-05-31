using System.Linq;
using Nebula.Parse;

namespace Nebula.SyntaxNodes
{
    internal class IndexerNode : StmtNode
    {
        public readonly string Lhs, Identifier;
        public readonly int IndexValue;
        public IndexerNode(string[] tokens) : base(tokens)
        {
            var lhsParser = new Var(tokens);
            var rhsParser = new Indexer(tokens.Skip(3));
            
            lhsParser.ParseLhs();
            Lhs = lhsParser.Lhs;
            Identifier = rhsParser.Identifier;
            IndexValue = rhsParser.IndexValue;
        }
    }
}