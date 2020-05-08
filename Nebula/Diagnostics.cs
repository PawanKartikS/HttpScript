using System;
using System.Collections.Generic;

namespace Nebula
{
    internal class Diagnostics
    {
        public int LineNum;
        public string SrcFile;

        public Diagnostics()
        {
        }

        public Diagnostics(int lineNum, string srcFile)
        {
            LineNum = lineNum;
            SrcFile = srcFile;
        }

        private string _GenError(string err) => $"Error L{LineNum} in {SrcFile}: {err}.";

        private string _GenWarn(string warn) => $"Warn L{LineNum} in {SrcFile}: {warn}.";
        
        public static void GenStackTrace(string err, string srcFile, string fnScope, Stack<string> fnStack)
        {
            Console.WriteLine($"\nStack trace -\nIn source file - {srcFile}.neb");
            Console.WriteLine($"{fnScope}() - {err}");
            
            while (fnStack.Count > 0)
                Console.WriteLine($"{fnStack.Pop()}()");
        }

        public string AccessVoidFnRes(string fnName)
        {
            return _GenError($"trying to access return value of void function - {fnName}");
        }

        public string ApiOverHttp()
        {
            return _GenWarn("api may not be accessed over HTTPS");
        }

        public string ApiResReadError(string endpoint)
        {
            return _GenError($"could not read api response for {endpoint}");
        }

        public string ArgCountMismatch(string fnName)
        {
            return _GenError($"argument count mismatch for - {fnName}");
        }

        public string ConditionConstants()
        {
            return _GenWarn("condition depends on values that do not change at runtime");
        }

        public string ConstantModif()
        {
            return _GenWarn("trying to modify symbol marked as constant");
        }

        public string DanglingStatement(string keyword)
        {
            return _GenError($"dangling statement - {keyword}");
        }

        public string ErrCodeNot200(string endpoint)
        {
            return _GenWarn($"warning: error code for {endpoint} is not 200");
        }

        public string FnRedeclaration(string fnName)
        {
            return _GenError($"function redeclaration - {fnName}");
        }

        public string InternalError(string err)
        {
            return _GenError($"internal error - {err}");
        }

        public string InvalidArgFor(string keyword)
        {
            return _GenError($"invalid argument for keyword - {keyword}");
        }

        public string InvalidPosUse()
        {
            return _GenError("move use keyword to the top");
        }

        public string InvokeError(string keyword)
        {
            return _GenError($"could not invoke - {keyword}");
        }

        public string MissingResCall()
        {
            return _GenWarn("missing res call to one or more API endpoints");
        }

        public string NestedFn(string chFn)
        {
            return _GenWarn($"nested function - function {chFn} lies inside another function");
        }

        public string OpenCodeBlock()
        {
            return _GenError("missing closure for code block");
        }

        public string UndeclaredFn(string fnName)
        {
            return _GenError($"undeclared function - {fnName}");
        }

        public string UndeclaredSymbol(string symbol)
        {
            return _GenError($"undeclared symbol - {symbol}");
        }

        public string UnExpectedKeyword(string keyword)
        {
            return _GenError($"unexpected keyword - {keyword}");
        }

        public string UnreachableCode(string fnName)
        {
            return _GenWarn($"unreachable code in function - {fnName}");
        }
    }
}