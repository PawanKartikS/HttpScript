using System.Linq;

namespace Nebula
{
    internal static class Tokens
    {
        public enum TokenType
        {
            // Keywords
            Def,
            Del,
            Else,
            End,
            Err,
            For,
            If,
            Print,
            Read,
            Res,
            Return,
            Url,
            Use,
            Var,

            // Arguments and parameters
            Const,
            Debug,
            Endpoint,
            Halt,
            Method,
            Scope,
            Success,
            Timeout,
            Warns,

            // Syntax
            BooleanFalse,
            Comment,
            Comma,
            CompEq,
            EqualTo,
            GreaterThan,
            GreaterThanEq,
            OpenBr,
            CloseBr,
            OpenPr,
            ClosePr,
            FieldAccess,
            LessThan,
            LessThanEq,
            NotEqual,
            
            // Data types
            StringLiteral,
            Numeric,
            Variable,
            
            // Statement types
            // These are to be used when we cannot exactly identify type using the keyword.
            // Example:
            // var x = 1      [Var]
            // var y = Foo()  [FnResult]
            FnCall,
            FnResult,
            
            Unknown
        };

        public static TokenType GetTokenType(string token)
        {
            if (token.Length >= 2 && token[0] == '\'' && token[^1] == '\'')
                return TokenType.StringLiteral;

            if (double.TryParse(token, out _))
                return TokenType.Numeric;

            var type = token switch
            {
                // Keywords
                "api"    => TokenType.Url,
                "def"    => TokenType.Def,
                "del"    => TokenType.Del,
                "else"   => TokenType.Else,
                "end"    => TokenType.End,
                "err"    => TokenType.Err,
                "for"    => TokenType.For,
                "if"     => TokenType.If,
                "print"  => TokenType.Print,
                "read"   => TokenType.Read,
                "res"    => TokenType.Res,
                "return" => TokenType.Return,
                "url"    => TokenType.Url,
                "use"    => TokenType.Use,
                "var"    => TokenType.Var,
                
                // Arguments and parameters
                "const"    => TokenType.Const,
                "debug"    => TokenType.Debug,
                "endpoint" => TokenType.Endpoint,
                "halt"     => TokenType.Halt,
                "method"   => TokenType.Method,
                "scope"    => TokenType.Scope,
                "success"  => TokenType.Success,
                "timeout"  => TokenType.Timeout,
                "warns"    => TokenType.Warns,
                
                // Syntax
                "!"   => TokenType.BooleanFalse,
                "#"   => TokenType.Comment,
                ","   => TokenType.Comma,
                "=="  => TokenType.CompEq,
                "="   => TokenType.EqualTo,
                ">"   => TokenType.GreaterThan,
                ">="  => TokenType.GreaterThanEq,
                "{"   => TokenType.OpenBr,
                "}"   => TokenType.CloseBr,
                "("   => TokenType.OpenPr,
                ")"   => TokenType.ClosePr,
                ":"   => TokenType.FieldAccess,
                "<"   => TokenType.LessThan,
                "<="  => TokenType.LessThanEq,
                "!="  => TokenType.NotEqual,
                _ => TokenType.Unknown
            };

            if (type != TokenType.Unknown)
                return type;

            if (token.Any(c => !char.IsLetterOrDigit(c) && c != '_'))
                return TokenType.Unknown;

            return (token.Length == 1 && char.IsLetter(token[0]) || token.Length > 1)
                ? TokenType.Variable
                : TokenType.Unknown;
        }
    }
}