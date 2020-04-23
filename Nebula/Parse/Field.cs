using System;
using System.Collections.Generic;

namespace Nebula.Parse
{
    internal class Field
    {
        public readonly string Lhs, Obj;

        public Field(IEnumerable<string> tokens, Tokens.TokenType property)
        {
            var stream = new TokenStream(tokens);
            stream.Ensure(property, true);
            
            if (stream.Peek() != Tokens.TokenType.Variable)
                throw new ArgumentException("parse: lhs for field must be a variable");

            (Lhs, _) = stream.Consume();
            stream.Ensure(Tokens.TokenType.EqualTo, true);

            // TODO: Properly validate Obj
            (Obj, _) = stream.Consume();
            stream.Ensure(Tokens.TokenType.FieldAccess, true);
            stream.Ensure(Tokens.TokenType.FieldAccess, true);

            stream.Ensure(property, true);
            stream.Ensure(0);
        }
    }
}