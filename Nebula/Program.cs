using System;
using System.Linq;

namespace Nebula
{
    internal static class Program
    {
        private static void Main(string[] args)
        {
            if (args.Length == 0)
                throw new ArgumentException("fatal: required source file(s) as arg");

            static bool IsValidSrcFile(string srcFile)
            {
                var beg = srcFile.LastIndexOf('.');
                return beg != -1 && srcFile.Substring(beg) == ".neb";
            }

            var srcFiles = args.Where(IsValidSrcFile).ToList();

            var ast = new Ast();
            foreach (var lexer in srcFiles.Select(srcFile => new Lexer(srcFile)))
            {
                while (lexer.LinesLeft())
                {
                    var tokens = lexer.Tokenize();
                    ast.AppendNode(tokens.ToArray(), lexer.ExtractFileName());
                }
            }

            ast.VisitNodes();
        }
    }
}