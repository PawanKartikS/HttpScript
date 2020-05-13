using System;
using System.Collections.Generic;

namespace Nebula.Parse
{
    internal class Field : TokenStream
    {
        public readonly string Lhs, Obj;

        public Field(IEnumerable<string> tokens, Tokens.TokenType property) : base(tokens)
        {
            Ensure(property, true);
            
            if (Peek() != Tokens.TokenType.Variable)
                throw new ArgumentException("parse: lhs for field must be a variable");

            (Lhs, _) = Consume();
            Ensure(Tokens.TokenType.EqualTo, true);

            // TODO: Properly validate Obj
            (Obj, _) = Consume();
            Ensure(Tokens.TokenType.FieldAccess, true);
            Ensure(Tokens.TokenType.FieldAccess, true);

            Ensure(property, true);
            Ensure(0);
        }
    }
}