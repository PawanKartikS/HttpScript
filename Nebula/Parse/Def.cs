using System;
using System.Collections.Generic;

namespace Nebula.Parse
{
    internal class Def
    {
        public readonly string FnName;
        public readonly List<string> FnArgs;

        public Def(IEnumerable<string> tokens)
        {
            FnArgs = new List<string>();
            var stream = new TokenStream(tokens);
            stream.Ensure(Tokens.TokenType.Def, true);
            
            var (fnName, type) = stream.Consume();
            if (type != Tokens.TokenType.Variable)
                throw new ArgumentException($"parse: invalid function name {fnName}");
            FnName = fnName;
            
            if (stream.Peek() == Tokens.TokenType.OpenPr)
                _ParseFnArgs(stream);
            
            stream.Ensure(Tokens.TokenType.FieldAccess, true);
            stream.Ensure(0);
        }

        private void _ParseFnArgs(TokenStream stream)
        {
            var args = Parse.ArgumentList(stream);
            foreach (var (arg, argType) in args)
            {
                if (argType != Tokens.TokenType.Variable)
                    throw new ArgumentException($"parse: invalid arg for func {FnName}");
                FnArgs.Add(arg);
            }
        }
    }
}