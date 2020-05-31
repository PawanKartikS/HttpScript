using System;
using System.Collections.Generic;

namespace Nebula.Parse
{
    internal class Def : TokenStream
    {
        public readonly string FnName;
        public Tokens.TokenType ReturnType;
        public readonly List<string> FnArgs;

        public Def(IEnumerable<string> tokens) : base(tokens)
        {
            ReturnType = Tokens.TokenType.Unknown;
            FnArgs = new List<string>();
            Ensure(Tokens.TokenType.Def, true);
            
            var (fnName, type) = Consume();
            if (type != Tokens.TokenType.Variable)
                throw new ArgumentException($"parse: invalid function name {fnName}");
            FnName = fnName;
            
            if (Peek() == Tokens.TokenType.OpenPr)
                _ParseFnArgs();
            
            Ensure(Tokens.TokenType.FieldAccess, true);
            if (!Empty())
                _ParseReturnType();
            
            Ensure(0);
        }

        private void _ParseFnArgs()
        {
            var args = Parse.ArgumentList(this);
            foreach (var (arg, argType) in args)
            {
                if (argType != Tokens.TokenType.Variable)
                    throw new ArgumentException($"parse: invalid arg for func {FnName}");
                FnArgs.Add(arg);
            }
        }
        
        private void _ParseReturnType()
        {
            var (returnType, type) = Consume();
            ReturnType = type switch
            {
                Tokens.TokenType.Str => Tokens.TokenType.StringLiteral,
                Tokens.TokenType.Num => Tokens.TokenType.Numeric,
                _ => throw new ArgumentException($"parse: invalid return type in func {FnName} - {returnType}")
            };
        }
    }
}