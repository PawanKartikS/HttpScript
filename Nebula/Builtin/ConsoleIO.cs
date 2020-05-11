using System;
using static Nebula.Tokens;
using System.Collections.Generic;

namespace Nebula.Builtin
{
    internal static class ConsoleIo
    {
        public static Tuple<string, TokenType> ReadLine(List<Tuple<string, TokenType>> fnArgs)
        {
            if (fnArgs.Count > 1)
                throw new ArgumentException("fatal: input() takes 0 or 1 arguments");

            if (fnArgs.Count != 1)
                return new Tuple<string, TokenType>(Console.ReadLine(), TokenType.StringLiteral);
            
            var (arg, _) = fnArgs[0];
            Console.Write(arg);
            return new Tuple<string, TokenType>(Console.ReadLine(), TokenType.StringLiteral);
        }
    }
}