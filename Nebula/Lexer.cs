using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Nebula
{
    internal class Lexer
    {
        private int _idx, _srcLineNum;
        private readonly string _srcFile;
        private readonly List<string> _srcLines;

        public Lexer(string srcFile)
        {
            _srcFile = srcFile;
            _idx = _srcLineNum = 0;

            if (srcFile == null)
                throw new ArgumentException($"fatal: arg srcFile is null");

            if (!File.Exists(srcFile))
                throw new FileNotFoundException($"Error: Specified {srcFile} not found!");

            if (srcFile.Substring(srcFile.LastIndexOf('.') + 1) != "neb")
                throw new ArgumentException($"fatal: invalid source file {srcFile}");

            _srcLines = new List<string>(File.ReadLines(srcFile));
        }

        public string ExtractFileName()
        {
            var beg = _srcFile.LastIndexOf('/');
            var end = _srcFile.IndexOf('.');

            beg = beg == -1 ? 0 : beg + 1;
            return _srcFile.Substring(beg, end - beg);
        }

        private static List<int> _GetQuoteIdx(string line)
        {
            var idx = new List<int>();
            for (var i = 0; i < line.Length; i++)
            {
                if (line[i] == '\'')
                    idx.Add(i);
            }

            return idx;
        }

        public bool LinesLeft() => _idx < _srcLines.Count;

        public IEnumerable<string> Tokenize()
        {
            _srcLineNum++;
            var tokens = new List<string>();
            var line = _srcLines[_idx++];
            if (line.Length == 0)
                return tokens;

            var beg = 0;
            var end = 0;
            var cbeg = line.IndexOf('#');
            var idx = _GetQuoteIdx(line);

            if (idx.Count == 0)
                return _TokenizeUsingRegex(line);

            // This is a comment line. Do not tokenize!
            if (cbeg == 0)
                return tokens;

            // String literal is inside the comment line.
            if (cbeg > 0 && idx[0] > cbeg)
                return _TokenizeUsingRegex(line.Substring(0, cbeg));

            if (idx.Count % 2 != 0)
                throw new ArgumentException($"lexer: did you forget to close a string literal - L{_srcLineNum}");

            for (var i = 0; i < idx.Count; i += 2)
            {
                if (end > 0)
                    end++;

                // Non string literal part which we tokenize with regex.
                tokens.AddRange(_TokenizeUsingRegex(line.Substring(end, idx[i] - end)));
                beg = idx[i];
                end = idx[i + 1];

                // String literal part which we add as it is to tokens.
                tokens.Add(line.Substring(beg, end - beg + 1));
            }

            if (end < line.Length - 1)
                tokens.AddRange(_TokenizeUsingRegex(line.Substring(end + 1)));

            return tokens;
        }

        private static IEnumerable<string> _TokenizeUsingRegex(string line)
        {
            return new List<string>(Regex.Split(line, @"([\[\]<>{}*!#{},;():='])|\s+")
                .Where(token => token.Length != 0));
        }
    }
}