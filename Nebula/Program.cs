using System;
using System.Linq;

namespace Nebula
{
    internal static class Program
    {
        private static void Main(string[] args)
        {
            if (args.Length != 1)
                throw new ArgumentException("fatal: required source file as arg");

            var ast = new Ast();
            var lexer = new Lexer(args[0]);

            while (lexer.LinesLeft())
            {
                var tokens = lexer.Tokenize();
                ast.AppendNode(tokens.ToArray(), args[0]);
            }

            ast.VisitNodes();
        }
    }
}