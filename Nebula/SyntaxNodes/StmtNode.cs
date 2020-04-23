using System.Collections.Generic;

namespace Nebula.SyntaxNodes
{
    internal class StmtNode
    {
        public int LineNum;
        public string SrcFile;
        
        public Tokens.TokenType Keyword;
        private readonly string[] _tokens;
        public readonly List<StmtNode> Alt, Body;

        protected StmtNode(string[] tokens)
        {
            _tokens = tokens;
            Alt = new List<StmtNode>();
            Body = new List<StmtNode>();
            Keyword = Tokens.GetTokenType(tokens[0]);
        }

        public string GetKeyword() => _tokens[0];
    }
}