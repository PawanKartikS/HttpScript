using System;
using System.Net;
using System.Linq;
using Nebula.SyntaxNodes;
using static Nebula.Tokens;
using System.Collections.Generic;

namespace Nebula
{
    internal class Ast : SymbolTable
    {
        private readonly Error _error;
        private readonly List<StmtNode> _body;
        private readonly Stack<StmtNode> _stack;
        private readonly Stack<string> _fnStack;
        private readonly Stack<int> _auxiliaryStack;
        private readonly Dictionary<string, StmtNode> _fnAddrTable;
        private readonly Stack<Tuple<string, TokenType>> _fnReturnStack;

        private HttpStatusCode _lastStatusCode;
        private string _currentFnScope, _currentSrcFile;
        private int _currNode, _currentDepth, _currentLineNum, _lr, _res, _urls;
        private bool _debugEnabled, _insideFn, _scopeEnabled, _warnsEnabled;

        public Ast()
        {
            _error = new Error(_currentLineNum, _currentFnScope);
            _body = new List<StmtNode>();
            _stack = new Stack<StmtNode>();
            _fnStack = new Stack<string>();
            _auxiliaryStack = new Stack<int>();
            _fnAddrTable = new Dictionary<string, StmtNode>();
            _fnReturnStack = new Stack<Tuple<string, TokenType>>();

            _currentFnScope = "Main";
            _lastStatusCode = HttpStatusCode.Unused;
            _currNode = _currentDepth = _currentLineNum = _lr = _res = _urls = 0;
            _debugEnabled = _insideFn = _scopeEnabled = _warnsEnabled = false;
        }

        public void AppendNode(string[] tokens, string currentSrcFile)
        {
            _currentLineNum++;
            _currentSrcFile = currentSrcFile;

            _error.LineNum = _currentLineNum;
            _error.SrcFile = _currentSrcFile;

            if (tokens.Length == 0)
                return;

            var keyword = tokens[0];
            var tokenType = GetTokenType(keyword);
            if (tokenType == TokenType.Comment)
                return;

            var node = Parse.Parse.ToNode(tokens);
            if (node == null)
                throw new ArgumentException(_error.UnExpectedKeyword(keyword));

            if (tokenType == TokenType.Def)
            {
                _InvokeDef(node);
                _insideFn = true;
            }

            if (node.Keyword != TokenType.Def && !_insideFn)
                throw new ArgumentException(_error.DanglingStatement(keyword));

            node.LineNum = _currentLineNum;
            node.SrcFile = _currentSrcFile;

            if (_stack.Count == 0)
            {
                _body.Add(node);
                if (!_IsBranch(tokenType))
                    return;

                if (tokenType != TokenType.Def && tokenType != TokenType.For && tokenType != TokenType.If)
                    throw new ArgumentException(_error.UnExpectedKeyword(keyword));

                _lr = 1;
                _stack.Push(node);
                return;
            }

            if (tokenType == TokenType.Def || tokenType == TokenType.For || tokenType == TokenType.If)
            {
                if (_lr == 1)
                    _stack.Peek().Body.Add(node);
                else
                    _stack.Peek().Alt.Add(node);

                _auxiliaryStack.Push(_lr);
                _lr = 1;
                _stack.Push(node);
                return;
            }

            if (tokenType == TokenType.Else)
            {
                if (_stack.Peek().Keyword != TokenType.If)
                    throw new ArgumentException(_error.UnExpectedKeyword(keyword));

                _lr = -1;
                var elseNode = (ElseNode) node;

                if (elseNode.TokensLength > 1)
                    AppendNode(tokens.Skip(1).ToArray(), currentSrcFile);
                return;
            }

            if (tokenType == TokenType.End)
            {
                var top = _stack.Peek().Keyword;
                if (top != TokenType.Def && top != TokenType.For && top != TokenType.If)
                    throw new ArgumentException(_error.UnExpectedKeyword(keyword));
                
                if (_stack.Peek().Keyword == TokenType.Def)
                    _insideFn = false;

                _lr = _auxiliaryStack.Count != 0 ? _auxiliaryStack.Pop() : 1;

                if (_warnsEnabled)
                    if (_stack.Peek().Body.Count == 0 && _stack.Peek().Alt.Count == 0)
                        _body.RemoveAt(_body.Count - 1);
                _stack.Pop();
                return;
            }
            
            if (_lr == 1)
                _stack.Peek().Body.Add(node);
            else
                _stack.Peek().Alt.Add(node);
        }

        private void _InitFnArgs(string fnName, IEnumerable<Tuple<string, TokenType>> fnArgs)
        {
            // def Foo(a, b, c)
            // Foo(arg1, arg2, 1.0)
            //
            // We need to bind the arguments as: arg1 -> a, arg2 -> b, 1.0 -> c
            // We do this by fetching values of arg1 and arg2 (1.0 is arg3), init a new frame and then
            // register them as new symbols so that they become available when controller enters Foo.
            var alias = new List<string>();
            var values = new List<Tuple<string, TokenType>>();
            var fnSignature = GetFnSignature(fnName);

            var idx = 0;
            foreach (var (arg, argType) in fnArgs)
            {
                var value = arg;
                var type = argType;

                if (argType == TokenType.Variable)
                {
                    var decl = GetSymbolValue(arg);
                    value = decl.Value;
                    type = decl.Type;
                }

                values.Add(new Tuple<string, TokenType>(value, type));
                alias.Add(fnSignature[idx]);
                idx++;
            }

            if (alias.Count != values.Count)
                throw new ArgumentException(_error.InternalError("alias.Count != values.Count"));

            // We init a new frame here so that the args are available for the function we're about to
            // immediately invoke.
            InitNewFrame();
            for (var i = 0; i < alias.Count; i++)
                RegisterSymbol(fnSignature[i], values[i].Item1, values[i].Item2, _currentDepth, fnName);
        }

        private void _InvokeDef(StmtNode node)
        {
            var defNode = (DefNode) node;
            if (_fnAddrTable.ContainsKey(defNode.FnName))
                throw new ArgumentException(_error.FnRedeclaration(defNode.FnName));

            _fnAddrTable[defNode.FnName] = defNode;
            RegisterFnSignature(defNode.FnName, defNode.FnArgs);
        }

        private void _InvokeDel(StmtNode node)
        {
            var delNode = (DelNode) node;
            if (IsRequestRegistered(delNode.Var, true))
                return;

            if (!IsSymbolRegistered(delNode.Var, true))
                throw new ArgumentException(_error.UndeclaredSymbol(delNode.Var));
        }

        private void _InvokeErr(StmtNode node)
        {
            var errNode = (ErrNode) node;
            var symbol = errNode.Lhs;
            var value = GetSymbolValue(errNode.ApiIdf).StatusCode;
            RegisterSymbol(symbol, value.ToString(), TokenType.StringLiteral, _currentDepth, _currentFnScope);
        }

        private void _InvokeFn(string fnName, IReadOnlyCollection<Tuple<string, TokenType>> fnArgs)
        {
            // Procedure for invoking a function -
            //   1. Check if function is declared.
            //   2. Check if function signature matches and the required args are provided.
            //   3. Init function args (push the args to stack via InitNewFrame()).
            //   4. Push current state to stack.
            //   5. Execute the function.

            if (!_fnAddrTable.ContainsKey(fnName))
                throw new ArgumentException(_error.UndeclaredFn(fnName));

            if (fnArgs.Count != GetFnSignature(fnName).Count)
                throw new ArgumentException(_error.ArgCountMismatch(fnName));

            _InitFnArgs(fnName, fnArgs);
            _fnStack.Push(_currentFnScope);

            _currentFnScope = fnName;
            var fnInitNode = _fnAddrTable[fnName];

            var hitRet = false;
            foreach (var ch in fnInitNode.Body)
            {
                if (hitRet)
                {
                    Console.WriteLine(_error.UnreachableCode(fnName));
                    break;
                }

                _VisitNode(ch);
                if (ch.Keyword == TokenType.Return)
                    hitRet = true;
            }

            _currentFnScope = _fnStack.Pop();
            PopFrame();
        }

        private void _InvokeFnRes(StmtNode node)
        {
            var stackSz = _fnReturnStack.Count;
            var fnResNode = (FnResNode) node;
            _InvokeFn(fnResNode.FnName, fnResNode.FnArgs);

            if (_fnReturnStack.Count <= stackSz)
                throw new ArgumentException(_error.AccessVoidFnRes(fnResNode.FnName));

            var (result, resultType) = _fnReturnStack.Pop();
            RegisterSymbol(fnResNode.Lhs, result, resultType, _currentDepth, _currentFnScope);
        }

        private bool _InvokeFor(StmtNode node) => _InvokeIf(node);

        private bool _InvokeIf(StmtNode node)
        {
            _currentDepth++;
            var ifNode = (IfNode) node;

            switch (ifNode.IfType)
            {
                case Parse.If.IfType.LastSuccess when _warnsEnabled && _lastStatusCode == HttpStatusCode.Unused:
                    return _lastStatusCode == HttpStatusCode.OK;

                case Parse.If.IfType.LastSuccess when ifNode.InverseEval:
                    return _lastStatusCode != HttpStatusCode.OK;

                case Parse.If.IfType.LastSuccess:
                    return _lastStatusCode == HttpStatusCode.OK;

                case Parse.If.IfType.VariableLookup:
                {
                    var exists = IsSymbolRegistered(ifNode.Lhs);
                    return ifNode.InverseEval ? !exists : exists;
                }

                case Parse.If.IfType.SimpleComparison:
                    break;

                default:
                    throw new ArgumentException();
            }

            var lhs = ifNode.Lhs;
            var rhs = ifNode.Rhs;
            var lhsType = ifNode.LhsType;
            var rhsType = ifNode.RhsType;

            if (ifNode.LhsType == TokenType.Variable)
            {
                var obj = GetSymbolValue(lhs);
                lhs = obj.Value;
                lhsType = obj.Type;
            }

            if (ifNode.RhsType == TokenType.Variable)
            {
                var obj = GetSymbolValue(rhs);
                rhs = obj.Value;
                rhsType = obj.Type;
            }

            if (ifNode.LhsType != TokenType.Variable && ifNode.RhsType != TokenType.Variable && _warnsEnabled)
                Console.WriteLine(_error.ConditionConstants());

            var result = Evaluator.Evaluate(lhs, rhs, lhsType, rhsType,
                ifNode.ComparisonOperator);
            return ifNode.InverseEval ? !result : result;
        }

        private void _InvokePrint(StmtNode node)
        {
            var printNode = (PrintNode) node;
            Console.WriteLine(printNode.ArgType == TokenType.Variable
                ? GetSymbolValue(printNode.Str).Value
                : printNode.Str);
        }

        private void _InvokeRead(StmtNode node)
        {
            var readNode = (ReadNode) node;
            var value = Console.ReadLine();
            RegisterSymbol(readNode.Symbol, value,
                double.TryParse(value, out _) ? TokenType.Numeric : TokenType.StringLiteral,
                _currentDepth, _currentFnScope);
        }

        private void _InvokeRes(StmtNode node)
        {
            _res++;
            var resNode = (ResNode) node;
            var symbol = resNode.Lhs;
            var urlNode = GetUrlNode(resNode.ApiIdf);
            var (value, statusCode) = Api.ReadResponse(urlNode.Endpoint, urlNode.Method, urlNode.Timeout);
            _lastStatusCode = statusCode;

            if (value == null && _warnsEnabled)
                throw new ArgumentException(_error.ApiResReadError(urlNode.Endpoint));

            if (statusCode != HttpStatusCode.OK && _warnsEnabled)
                Console.WriteLine(_error.ErrCodeNot200(urlNode.Endpoint));

            RegisterSymbol(symbol, value, TokenType.StringLiteral, _currentDepth, _currentFnScope, false, statusCode);
        }

        private void _InvokeReturn(StmtNode node)
        {
            var returnNode = (ReturnNode) node;
            if (returnNode.Arg == null)
                return;

            var val = returnNode.Arg;
            var valType = returnNode.ArgType;

            if (returnNode.ArgType == TokenType.Variable)
            {
                var decl = GetSymbolValue(val);
                val = decl.Value;
                valType = decl.Type;
            }

            _fnReturnStack.Push(new Tuple<string, TokenType>(val, valType));
        }

        private void _InvokeUrl(StmtNode node)
        {
            _urls++;
            var urlNode = (UrlNode) node;
            var symbol = urlNode.Lhs;
            if (!urlNode.Endpoint.Contains("."))
            {
                urlNode.Endpoint = GetSymbolValue(urlNode.Endpoint).Value;
                urlNode.Endpoint = string.Concat(urlNode.Endpoint.Where(c => !char.IsWhiteSpace(c)));
            }

            if (_warnsEnabled)
                if (!urlNode.Endpoint.Contains("https"))
                    Console.WriteLine(_error.ApiOverHttp());

            RegisterRequest(symbol, urlNode);
        }

        private void _InvokeUse(StmtNode node)
        {
            var useNode = (UseNode) node;
            var arg = useNode.Arg;

            if (arg == TokenType.Debug)
                _debugEnabled = true;
            else if (arg == TokenType.Scope)
                _scopeEnabled = true;
            else if (arg == TokenType.Warns)
            {
                if (_currNode > 1)
                    throw new ArgumentException(_error.InvalidPosUse());
                _warnsEnabled = true;
            }
            else
                throw new ArgumentException(_error.InvalidArgFor("use"));
        }

        private void _InvokeVar(StmtNode node)
        {
            var varNode = (VarNode) node;
            var rhs = varNode.Rhs;
            var rhsType = varNode.RhsType;

            if (varNode.RhsType == TokenType.Variable)
            {
                var obj = GetSymbolValue(varNode.Rhs);
                rhs = obj.Value;
                rhsType = obj.Type;
            }

            RegisterSymbol(varNode.Lhs, rhs, rhsType, _currentDepth, _currentFnScope, varNode.IsConstant);
        }

        private static bool _IsBranch(TokenType type)
        {
            return type switch
            {
                TokenType.Def  => true,
                TokenType.For  => true,
                TokenType.If   => true,
                TokenType.Else => true,
                TokenType.End  => true,
                _ => false
            };
        }

        public void VisitNodes(bool dumpSymbolTable = false)
        {
            if (_stack.Count != 0)
                throw new ArgumentException(_error.OpenCodeBlock());

            var body = _fnAddrTable["Main"];
            InitNewFrame();

            foreach (var node in body.Body)
            {
                _currNode++;
                _VisitNode(node);
            }

            PopFrame();

            if (_scopeEnabled)
                ScopeCleanUp(_currentDepth);

            if (_urls > _res && _warnsEnabled)
                Console.WriteLine(_error.MissingResCall());

            if (_debugEnabled && dumpSymbolTable)
                DumpSymbolTable();
        }

        private void _VisitNode(StmtNode node)
        {
            _error.SrcFile = node.SrcFile;
            _error.LineNum = node.LineNum;

            var nodeType = node.Keyword;
            if (nodeType == TokenType.Del)
                _InvokeDel(node);
            else if (nodeType == TokenType.Err)
                _InvokeErr(node);
            else if (nodeType == TokenType.FnCall)
            {
                var fnCallNode = (FnCallNode) node;
                _InvokeFn(fnCallNode.FnName, fnCallNode.FnArgs);
            }
            else if (nodeType == TokenType.FnResult)
                _InvokeFnRes(node);
            else if (nodeType == TokenType.For)
            {
                var eval = _InvokeFor(node);
                while (eval)
                {
                    foreach (var child in node.Body)
                        _VisitNode(child);
                    eval = _InvokeFor(node);
                }

                _currentDepth--;
                if (_scopeEnabled)
                    ScopeCleanUp(_currentDepth);
            }
            else if (nodeType == TokenType.If)
            {
                if (_InvokeIf(node))
                    foreach (var child in node.Body)
                        _VisitNode(child);
                else
                    foreach (var child in node.Alt)
                        _VisitNode(child);

                _currentDepth--;
                if (_scopeEnabled)
                    ScopeCleanUp(_currentDepth);
            }
            else if (nodeType == TokenType.Print)
                _InvokePrint(node);
            else if (nodeType == TokenType.Read)
                _InvokeRead(node);
            else if (nodeType == TokenType.Res)
                _InvokeRes(node);
            else if (nodeType == TokenType.Return)
                _InvokeReturn(node);
            else if (nodeType == TokenType.Url)
                _InvokeUrl(node);
            else if (nodeType == TokenType.Use)
                _InvokeUse(node);
            else if (nodeType == TokenType.Var)
                _InvokeVar(node);
            else if (nodeType != TokenType.Def)
                throw new ArgumentException(_error.InvokeError(node.GetKeyword()));
        }
    }
}