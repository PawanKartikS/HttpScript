using System;
using System.Linq;
using Nebula.SyntaxNodes;
using static Nebula.Tokens;
using System.Collections.Generic;

namespace Nebula
{
    internal class Ast : Evaluator
    {
        private readonly Diagnostics _diagnostics;
        private readonly List<StmtNode> _body;
        private readonly Stack<StmtNode> _stack;
        private readonly Stack<int> _auxiliaryStack;

        private bool _insideFn;
        private int _currLineNum, _lr;

        public Ast() : base(null)
        {
            _diagnostics = new Diagnostics();
            _body = new List<StmtNode>();
            _stack = new Stack<StmtNode>();
            _auxiliaryStack = new Stack<int>();

            _insideFn = false;
            _currLineNum = _lr = 0;
        }

        public void AppendNode(string[] tokens, string currSrcFile)
        {
            _currLineNum++;
            // TODO: Shouldn't happen. Lexer should skip such lines.
            if (tokens.Length == 0)
                return;
            
            // In case AppendNode() bails out.
            SetSourceFile(currSrcFile);

            // Set up required values for diagnostics.
            // TODO: Diagnostics must be accessible for Parser through Parse.ToNode().
            _diagnostics.LineNum = _currLineNum;
            _diagnostics.SrcFile = currSrcFile;

            var keyword = tokens[0];
            var tokenType = GetTokenType(keyword);
            
            if (tokenType == TokenType.Comment)
                return;

            var node = Parse.Parse.ToNode(tokens);
            // We do not recognize this keyword.
            if (node == null)
                throw new ArgumentException(_diagnostics.UnExpectedKeyword(keyword));

            // Def is an exception. It is invoked in P-1 and not P-2.
            if (tokenType == TokenType.Def)
            {
                if (_insideFn)
                    throw new ArgumentException(_diagnostics.NestedFn(tokens[1]));
                
                EvaluateDef(node);
                _insideFn = true;
            }

            // Every line must fall under a valid function area. The only exception is Def which
            // marks the beginning of such area.
            if (node.Keyword != TokenType.Def && !_insideFn)
                throw new ArgumentException(_diagnostics.DanglingStatement(keyword));

            // In case EvaluateNode() bails out (in P-2).
            node.LineNum = _currLineNum;
            node.SrcFile = currSrcFile;

            if (_stack.Count == 0)
            {
                _body.Add(node);
                if (!_IsBranch(tokenType))
                    return;

                // _IsBranch() returns true for Else and End.
                var _ = tokenType switch
                {
                    TokenType.Def    => true,
                    TokenType.For    => true,
                    TokenType.If     => true,
                    TokenType.OpenBr => true,
                    // Do not push Else & End to stack.
                    _ => throw new ArgumentException(_diagnostics.UnExpectedKeyword(keyword))
                };

                _lr = 1;
                _stack.Push(node);
                return;
            }
            
            // Branch required
            // Next incoming statements are appended to the TOS's body/alt.
            if (tokenType == TokenType.Def ||
                tokenType == TokenType.For ||
                tokenType == TokenType.If  ||
                // We're opening a resource intensive block.
                tokenType == TokenType.OpenBr)
            {
                if (_lr == 1)
                    _stack.Peek().Body.Add(node);
                else
                    _stack.Peek().Alt.Add(node);

                _auxiliaryStack.Push(_lr);
                // Transition to new state.
                _lr = 1;
                _stack.Push(node);
                return;
            }

            if (tokenType == TokenType.Else)
            {
                // Else can occur only under if.
                if (_stack.Peek().Keyword != TokenType.If)
                    throw new ArgumentException(_diagnostics.UnExpectedKeyword(keyword));

                _lr = -1;
                var elseNode = (ElseNode) node;

                // Probably else-if.
                // Recursively parse and add the if to AST.
                if (elseNode.TokensLength > 1)
                {
                    AppendNode(new[] {"end"}, currSrcFile);
                    AppendNode(tokens.Skip(1).ToArray(), currSrcFile);
                }
                
                return;
            }

            // End of resource intensive block.
            if (tokenType == TokenType.CloseBr)
            {
                var top = _stack.Peek().Keyword;
                if (top != TokenType.OpenBr)
                    throw new ArgumentException(_diagnostics.UnExpectedKeyword(keyword));

                _stack.Pop();
                return;
            }

            if (tokenType == TokenType.End)
            {
                var top = _stack.Peek().Keyword;
                // End can occur only for Def, For and If.
                var _ = top switch
                {
                    TokenType.Def     => true,
                    TokenType.For     => true,
                    TokenType.If      => true,
                    _ => throw new ArgumentException(_diagnostics.UnExpectedKeyword(keyword))
                };
                
                // Close the function body
                if (_stack.Peek().Keyword == TokenType.Def)
                    _insideFn = false;

                // Restore previous state
                _lr = _auxiliaryStack.Count != 0 ? _auxiliaryStack.Pop() : 1;

                // If the previous node's main body and alt body is empty, discard the node.
                // This eliminates extra recursion (EvaluateNode()) for both lch and rch.
                if (_stack.Peek().Body.Count == 0 && _stack.Peek().Alt.Count == 0)
                    _body.RemoveAt(_body.Count - 1);
                
                _stack.Pop();
                return;
            }
            
            if (_lr == 1)
                _stack.Peek().Body.Add(node);
            else
                _stack.Peek().Alt.Add(node);
        }
        
        public void Finish()
        {
            if (_stack.Count != 0)
                throw new ArgumentException(_diagnostics.OpenCodeBlock());

            try
            {
                EvaluateProgram();
            }
            catch (Exception e)
            {
                AbortProgram(e.Message);
            }
        }

        private static bool _IsBranch(TokenType type)
        {
            return type switch
            {
                TokenType.Def    => true,
                TokenType.For    => true,
                TokenType.If     => true,
                TokenType.Else   => true,
                TokenType.End    => true,
                TokenType.OpenBr => true,
                _ => false
            };
        }
    }
}