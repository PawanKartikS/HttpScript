using System;
using System.Linq;
using static Nebula.Tokens;
using System.Collections.Generic;

namespace Nebula.Builtin
{
    internal static class StringMethods
    {
        public static Tuple<string, TokenType> StringLength(List<Tuple<string, TokenType>> fnArgs)
        {
            if (fnArgs.Count != 1)
                throw new ArgumentException("fatal: strlen() takes only 1 argument");

            var (arg, argType) = fnArgs[0];
            if (argType != TokenType.StringLiteral)
                throw new ArgumentException("fatal: strlen() takes string as an argument");
            
            return new Tuple<string, TokenType>(arg.Length.ToString(), TokenType.Numeric);
        }
        
        public static Tuple<string, TokenType> StringRev(List<Tuple<string, TokenType>> fnArgs)
        {
            if (fnArgs.Count != 1)
                throw new ArgumentException("fatal: strrev() takes only 1 argument");
            
            var (arg, argType) = fnArgs[0];
            if (argType != TokenType.StringLiteral)
                throw new ArgumentException("fatal: strrev() takes string as an argument");
            
            var s = new string(arg.Reverse().ToArray());
            return new Tuple<string, TokenType>(s, TokenType.Numeric);
        }
    }
}