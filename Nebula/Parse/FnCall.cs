using System;
using System.Collections.Generic;

namespace Nebula.Parse
{
    internal class FnCall
    {
        public readonly string FnName;
        public readonly List<Tuple<string, Tokens.TokenType>> FnArgs;

        public FnCall(IEnumerable<string> tokens)
        {
            FnArgs = new List<Tuple<string, Tokens.TokenType>>();
            var stream = new TokenStream(tokens);
            var (fnName, type) = stream.Consume();
            
            if (type != Tokens.TokenType.Variable)
                throw new ArgumentException($"parse: invalid function name {fnName}");
            FnName = fnName;
            
            _ParseFnArgs(stream);
            stream.Ensure(0);
        }

        private void _ParseFnArgs(TokenStream stream)
        {
            var args = Parse.ArgumentList(stream);
            foreach (var (arg, argType) in args)
            {
                _ = argType switch
                {
                    Tokens.TokenType.StringLiteral => true,
                    Tokens.TokenType.Variable      => true,
                    Tokens.TokenType.Numeric       => true,
                    _  => throw new ArgumentException($"fatal: invalid arg {arg} for fn {FnName}")
                };
                
                FnArgs.Add(new Tuple<string, Tokens.TokenType>(arg, argType));
            }
        }
    }
}