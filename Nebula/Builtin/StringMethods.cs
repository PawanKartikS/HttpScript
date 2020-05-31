using System;
using System.Linq;
using static Nebula.Tokens;
using System.Collections.Generic;

namespace Nebula.Builtin
{
    internal static class StringMethods
    {
        public enum StrConv
        {
            Atoi,
            Itoa
        }
        
        public static Tuple<string, TokenType> Strcmp(List<Tuple<string, TokenType>> fnArgs)
        {
            if (fnArgs.Count != 2)
                throw new ArgumentException("fatal: strcmp() takes 2 arguments");
            
            if (fnArgs[0].Item2 != TokenType.StringLiteral || fnArgs[1].Item2 != TokenType.StringLiteral)
                throw new ArgumentException("fatal: strcmp() takes strings as both arguments");

            var s1 = fnArgs[0].Item1;
            var s2 = fnArgs[1].Item1;
            var cmp = string.Compare(s1, s2, StringComparison.Ordinal);

            return new Tuple<string, TokenType>(cmp.ToString(), TokenType.Numeric);
        }

        public static Tuple<string, TokenType> Strconv(List<Tuple<string, TokenType>> fnArgs, StrConv convMethod)
        {
            // This method just changes the targetType (just the data type field in the symbol table).
            // Any function invoked with this variable as an argument does the conversions as required.
            var method = convMethod == StrConv.Atoi ?
                "atoi" :
                "itoa";
            
            var targetType = convMethod == StrConv.Atoi ?
                TokenType.Numeric :
                TokenType.StringLiteral;

            if (fnArgs.Count != 1)
                throw new ArgumentException($"fatal: {method}() takes only 1 argument");

            var (arg, _) = fnArgs[0];
            return new Tuple<string, TokenType>(arg, targetType);
        }

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