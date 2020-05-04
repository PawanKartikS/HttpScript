using System;
using Nebula.SyntaxNodes;
using static Nebula.Tokens;
using System.Collections.Generic;

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

        public static StmtNode ToNode(string[] tokens)
        {
            var keyword = tokens[0];
            var tokenType = GetTokenType(keyword);

            switch (tokenType)
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
                case TokenType.Var when tokens.Length > 4 && GetTokenType(tokens[3]) == TokenType.Strlen:
                    return new FnResNode(tokens) {Keyword = TokenType.FnResult};
                case TokenType.Var when tokens.Length > 4 && GetTokenType(tokens[4]) == TokenType.OpenPr:
                    return new FnResNode(tokens) {Keyword = TokenType.FnResult};
                case TokenType.Var:
                    return new VarNode(tokens);
            }

            if (tokens.Length >= 2 && GetTokenType(tokens[1]) == TokenType.OpenPr)
                // Keyword is set by StmtNode(). But this is an exception case. Override the keyword.
                return new FnCallNode(tokens) {Keyword = TokenType.FnCall};

            return null;
        }
    }
}