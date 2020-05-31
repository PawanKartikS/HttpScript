using System;
using System.Linq;
using Nebula.SyntaxNodes;
using static Nebula.Tokens;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Nebula.Parse
{
    internal static class Parse
    {
        public static IEnumerable<Tuple<string, TokenType>> ArgumentList(TokenStream stream)
        {
            var args = new List<Tuple<string, TokenType>>();

            stream.Ensure(TokenType.OpenPr, true);
            if (stream.Peek() == TokenType.ClosePr)
            {
                stream.Consume();
                return args;
            }

            while (stream.Peek() != TokenType.ClosePr)
            {
                var (arg, argType) = stream.Consume();
                args.Add(new Tuple<string, TokenType>(arg, argType));

                if (stream.Peek() == TokenType.ClosePr)
                    continue;

                if (stream.Peek() == TokenType.Comma)
                {
                    if (stream.LookAhead() == TokenType.ClosePr)
                        throw new ArgumentException("parse: invalid token after comma");
                    stream.Consume();
                }
                else
                    throw new ArgumentException($"parse: invalid token after arg {arg}");
            }

            stream.Ensure(TokenType.ClosePr, true);
            return args;
        }
        
        private static bool _IsFnCall(IReadOnlyList<string> tokens)
        {
            // func()
            // func(arg)
            // func(arg,...)
            return tokens.Count >= 3 &&
                   GetTokenType(tokens[1]) == TokenType.OpenPr &&
                   GetTokenType(tokens[^1]) == TokenType.ClosePr;
        }

        private static bool _IsFnResult(IReadOnlyCollection<string> tokens)
        {
            // var res = func()
            // var res = func(arg)
            // var res = func(arg,...)
            return tokens.Count >= 6 &&
                   _IsFnCall(tokens.Skip(3).ToImmutableList());
        }

        private static bool _IsIndexer(IReadOnlyList<string> tokens)
        {
            // var idf = arr[idx]
            return tokens.Count == 7 &&
                   GetTokenType(tokens[^1]) == TokenType.SqBrClose &&
                   GetTokenType(tokens[^3]) == TokenType.SqBrOpen;
        }

        private static bool _IsUnary(IReadOnlyList<string> tokens, out TokenType unaryType)
        {
            if (tokens.Count != 1)
            {
                unaryType = TokenType.Unknown;
                return false;
            }

            var token = tokens[0];
            var x = GetTokenType(token[^1]);
            var y = GetTokenType(token[^2]);

            switch (x)
            {
                case TokenType.Inc when y == TokenType.Inc:
                    unaryType = TokenType.PostIncOp;
                    return true;
                case TokenType.Dec when y == TokenType.Dec:
                    unaryType = TokenType.PostDecOp;
                    return true;
                default:
                    unaryType = TokenType.Unknown;
                    return false;
            }
        }

        public static StmtNode ToNode(string[] tokens)
        {
            var keyword = tokens[0];
            var keywordType = GetTokenType(keyword);

            switch (keywordType)
            {
                case TokenType.Def:
                    return new DefNode(tokens);
                case TokenType.Del:
                    return new DelNode(tokens);
                case TokenType.Err:
                    return new ErrNode(tokens);
                case TokenType.Else:
                    return new ElseNode(tokens);
                case TokenType.End:
                    return new EndNode(tokens);
                case TokenType.For:
                    // IfNode and ForNode share same parser. Just that body of For is executed till
                    // the condition becomes false.
                    return new IfNode(tokens) {Keyword = TokenType.For};
                case TokenType.If:
                    return new IfNode(tokens);
                case TokenType.OpenBr:
                    return new ScopeNode(tokens);
                case TokenType.CloseBr:
                    return new ScopeNode(tokens) {Keyword = TokenType.CloseBr};
                case TokenType.Print:
                    return new PrintNode(tokens);
                case TokenType.Read:
                    return new ReadNode(tokens);
                case TokenType.Res:
                    return new ResNode(tokens);
                case TokenType.Return:
                    return new ReturnNode(tokens);
                case TokenType.Url:
                    return new UrlNode(tokens);
                case TokenType.Use:
                    return new UseNode(tokens);
                
                // Var requires some extra work
                case TokenType.Var when _IsFnResult(tokens):
                    return new FnResNode(tokens) {Keyword = TokenType.FnResult};
                case TokenType.Var when _IsIndexer(tokens):
                    return new IndexerNode(tokens) {Keyword = TokenType.Indexer};
                // Try out the usual declaration parsing. If this fails, it's an invalid statement.
                case TokenType.Var:
                    return new VarNode(tokens);
                
                // Try out everything we support
                default:
                    if (_IsFnCall(tokens))
                        return new FnCallNode(tokens) {Keyword = TokenType.FnCall};

                    if (_IsUnary(tokens, out var unaryType))
                        return new UnaryNode(tokens) {Keyword = unaryType};

                    return null;
            }
        }
    }
}