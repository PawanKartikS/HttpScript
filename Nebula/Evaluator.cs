using System;
using System.Net;
using System.Linq;
using Nebula.SyntaxNodes;
using static Nebula.Tokens;
using System.Collections.Generic;

namespace Nebula
{
    internal class Evaluator : SymbolTable
    {
        private readonly Diagnostics _diagnostics;
        private readonly Stack<string> _fnStack;
        private readonly Dictionary<string, StmtNode> _fnAddrTable;
        private readonly Stack<Tuple<string, TokenType>> _fnReturnStack;
        
        private HttpStatusCode _lastStatusCode;
        private bool _scopeEnabled, _warnsEnabled;
        private string _currFnScope, _currSrcFile;
        private int _currNode, _currDepth, _res, _urls;

        protected Evaluator(string currSrcFile)
        {
            _diagnostics = new Diagnostics();
            _fnStack = new Stack<string>();
            _fnAddrTable = new Dictionary<string, StmtNode>();
            _fnReturnStack = new Stack<Tuple<string, TokenType>>();

            _currFnScope = "Main";
            _currSrcFile = currSrcFile;
            _scopeEnabled = _warnsEnabled = false;
            _lastStatusCode = HttpStatusCode.Unused;
            _currNode = _currDepth = _res = _urls = 0;
        }

        protected void AbortProgram(string err) => Diagnostics.GenStackTrace(err, _currSrcFile, _currFnScope, _fnStack);
        
        private static bool _EvaluateCondition(string lhs, string rhs, TokenType lhsType, TokenType rhsType,
            TokenType comparisonOperator)
        {
            if (lhsType != rhsType)
                throw new ArgumentException("evaluator: cannot compare between 2 different types");

            if (comparisonOperator == TokenType.CompEq)
                return lhs.Equals(rhs);
            if (comparisonOperator == TokenType.NotEqual)
                return !lhs.Equals(rhs);

            if (lhsType == TokenType.StringLiteral)
            {
                if (comparisonOperator == TokenType.LessThan)
                    return string.Compare(lhs, rhs, StringComparison.Ordinal) < 0;
                if (comparisonOperator == TokenType.LessThanEq)
                    return string.Compare(lhs, rhs, StringComparison.Ordinal) <= 0;
                if (comparisonOperator == TokenType.GreaterThan)
                    return string.Compare(lhs, rhs, StringComparison.Ordinal) > 0;
                if (comparisonOperator == TokenType.GreaterThanEq)
                    return string.Compare(lhs, rhs, StringComparison.Ordinal) >= 0;
            }

            return comparisonOperator switch
            {
                TokenType.LessThan => (double.Parse(lhs) < double.Parse(rhs)),
                TokenType.LessThanEq => (double.Parse(lhs) <= double.Parse(rhs)),
                TokenType.GreaterThan => (double.Parse(lhs) > double.Parse(rhs)),
                TokenType.GreaterThanEq => (double.Parse(lhs) >= double.Parse(rhs)),
                _ => false
            };
        }
        
        private void _InitFnArgs(string fnName, IEnumerable<Tuple<string, TokenType>> fnArgs)
        {
            // def Foo(a, b, c)
            // Foo(arg1, arg2, 1.0)
            //
            // We need to bind the arguments as: arg1 -> a, arg2 -> b, 1.0 -> c
            // We do this by fetching values of arg1 and arg2 (1.0 is arg3), init a new frame and then
            // register them as new symbols so that they become available when controller enters Foo.
            var alias = GetFnSignature(fnName);
            var values = fnArgs.ToList();

            if (alias.Count != values.Count)
                throw new ArgumentException(_diagnostics.InternalError("alias.Count != values.Count"));

            // We init a new frame here so that the args are available for the function we're about to
            // immediately invoke.
            InitNewFrame();
            for (var i = 0; i < alias.Count; i++)
                RegisterSymbol(alias[i], values[i].Item1, values[i].Item2, _currDepth, fnName);
        }
        
        private void _EvaluateBuiltInFn(string fnName, IEnumerable<Tuple<string, TokenType>> fnArgs)
        {
            // Avoid HashSet<> to prevent additional overhead!
            var res = fnName switch
            {
                "strlen" => Builtin.StringMethods.StringLength(fnArgs.ToList()),
                "strrev" => Builtin.StringMethods.StringRev(fnArgs.ToList()),
                _ => null
            };

            if (res == null)
                throw new ArgumentException(_diagnostics.UndeclaredFn(fnName));
            _fnReturnStack.Push(res);
        }
        
        private void _EvaluateDec(StmtNode node)
        {
            var incNode = (UnaryNode) node;
            var d = _EvaluateUnary(node);
            
            var newVal = (int.Parse(d.Value) - 1).ToString();
            RegisterSymbol(incNode.Arg, newVal, TokenType.Numeric, d.Depth, _currFnScope);
        }

        protected void EvaluateDef(StmtNode node)
        {
            var defNode = (DefNode) node;
            if (_fnAddrTable.ContainsKey(defNode.FnName))
                throw new ArgumentException(_diagnostics.FnRedeclaration(defNode.FnName));

            var fnName = defNode.FnName;
            if (fnName != "Main")
                fnName = $"{_currSrcFile}.{defNode.FnName}";
            _fnAddrTable[fnName] = defNode;
            RegisterFnSignature(fnName, defNode.FnArgs);
        }

        private void _EvaluateDel(StmtNode node)
        {
            var delNode = (DelNode) node;
            if (IsRequestRegistered(delNode.Var, true))
                return;

            if (!IsSymbolRegistered(delNode.Var, true))
                throw new ArgumentException(_diagnostics.UndeclaredSymbol(delNode.Var));
        }

        private void _EvaluateErr(StmtNode node)
        {
            var errNode = (ErrNode) node;
            var symbol = errNode.Lhs;
            var value = GetSymbolValue(errNode.ApiIdf).StatusCode;
            RegisterSymbol(symbol, value.ToString(), TokenType.StringLiteral, _currDepth, _currFnScope);
        }

        private void _EvaluateFn(string fnName, IEnumerable<Tuple<string, TokenType>> fnArgs)
        {
            var args = _ResolveFnArgs(fnArgs);
            // Procedure for invoking a function -
            //   1. Check if function is declared.
            //   2. Check if function signature matches and the required args are provided.
            //   3. Init function args (push the args to stack via InitNewFrame()).
            //   4. Push current state to stack.
            //   5. Execute the function.

            if (!_fnAddrTable.ContainsKey(fnName))
            {
                // Try and see if this is a builtin function.
                // If it is not, let _EvaluateBuiltInFn handle it.
                _EvaluateBuiltInFn(fnName, args);
                return;
            }

            if (args.Count != GetFnSignature(fnName).Count)
                throw new ArgumentException(_diagnostics.ArgCountMismatch(fnName));

            _InitFnArgs(fnName, args);
            _fnStack.Push(_currFnScope);

            _currFnScope = fnName;
            var fnInitNode = _fnAddrTable[fnName];

            var hitRet = false;
            foreach (var ch in fnInitNode.Body)
            {
                if (hitRet)
                {
                    Console.WriteLine(_diagnostics.UnreachableCode(fnName));
                    break;
                }

                _EvaluateNode(ch);
                if (ch.Keyword == TokenType.Return)
                    hitRet = true;
            }

            _currFnScope = _fnStack.Pop();
            PopFrame();
        }

        private void _EvaluateFnRes(StmtNode node)
        {
            var stackSz = _fnReturnStack.Count;
            var fnResNode = (FnResNode) node;
            _EvaluateFn(fnResNode.FnName, fnResNode.FnArgs);

            if (_fnReturnStack.Count <= stackSz)
                throw new ArgumentException(_diagnostics.AccessVoidFnRes(fnResNode.FnName));

            var (result, resultType) = _fnReturnStack.Pop();
            RegisterSymbol(fnResNode.Lhs, result, resultType, _currDepth, _currFnScope);
        }

        private bool _EvaluateFor(StmtNode node) => _EvaluateIf(node);

        private bool _EvaluateIf(StmtNode node)
        {
            _currDepth++;
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
                Console.WriteLine(_diagnostics.ConditionConstants());

            var result = Evaluator._EvaluateCondition(lhs, rhs, lhsType, rhsType,
                ifNode.ComparisonOperator);
            return ifNode.InverseEval ? !result : result;
        }

        private void _EvaluateInc(StmtNode node)
        {
            var incNode = (UnaryNode) node;
            var d = _EvaluateUnary(node);
            
            var newVal = (int.Parse(d.Value) + 1).ToString();
            RegisterSymbol(incNode.Arg, newVal, TokenType.Numeric, d.Depth, _currFnScope);
        }

        private void _EvaluatePrint(StmtNode node)
        {
            var printNode = (PrintNode) node;
            Console.WriteLine(printNode.ArgType == TokenType.Variable
                ? GetSymbolValue(printNode.Str).Value
                : printNode.Str);
        }

        private void _EvaluateRead(StmtNode node)
        {
            var readNode = (ReadNode) node;
            var value = Console.ReadLine();
            RegisterSymbol(readNode.Symbol, value,
                double.TryParse(value, out _) ? TokenType.Numeric : TokenType.StringLiteral,
                _currDepth, _currFnScope);
        }

        private void _EvaluateRes(StmtNode node)
        {
            _res++;
            var resNode = (ResNode) node;
            var symbol = resNode.Lhs;
            var urlNode = GetUrlNode(resNode.ApiIdf);
            var (value, statusCode) = Api.ReadResponse(urlNode.Endpoint, urlNode.Method, urlNode.Timeout);
            _lastStatusCode = statusCode;

            if (value == null && _warnsEnabled)
                throw new ArgumentException(_diagnostics.ApiResReadError(urlNode.Endpoint));

            if (statusCode != HttpStatusCode.OK && _warnsEnabled)
                Console.WriteLine(_diagnostics.ErrCodeNot200(urlNode.Endpoint));

            RegisterSymbol(symbol, value, TokenType.StringLiteral, _currDepth, _currFnScope, false, statusCode);
        }

        private void _EvaluateReturn(StmtNode node)
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

        private void _EvaluateUrl(StmtNode node)
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
                    Console.WriteLine(_diagnostics.ApiOverHttp());

            RegisterRequest(symbol, urlNode);
        }

        private void _EvaluateUse(StmtNode node)
        {
            var useNode = (UseNode) node;
            var arg = useNode.Arg;

            if (arg == TokenType.Scope)
                _scopeEnabled = true;
            else if (arg == TokenType.Warns)
            {
                if (_currNode > 1)
                    throw new ArgumentException(_diagnostics.InvalidPosUse());
                _warnsEnabled = true;
            }
            else
                throw new ArgumentException(_diagnostics.InvalidArgFor("use"));
        }

        private void _EvaluateVar(StmtNode node)
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

            RegisterSymbol(varNode.Lhs, rhs, rhsType, _currDepth, _currFnScope, varNode.IsConstant);
        }

        private Decl _EvaluateUnary(StmtNode node)
        {
            var incNode = (UnaryNode) node;
            var d = GetSymbolValue(incNode.Arg);
            
            if (d.Type != TokenType.Numeric)
                throw new ArgumentException("fatal: Numeric type is required for increment op");
            
            if (d.IsConstant)
                throw new ArgumentException(_diagnostics.ConstantModif());

            return d;
        }

        private List<Tuple<string, TokenType>> _ResolveFnArgs(IEnumerable<Tuple<string, TokenType>> fnArgs)
        {
            var args = new List<Tuple<string, TokenType>>();
            foreach (var (arg, argType) in fnArgs)
            {
                var i1 = arg;
                var i2 = argType;
                if (argType == TokenType.Variable)
                {
                    var d = GetSymbolValue(arg);
                    i1 = d.Value;
                    i2 = d.Type;
                }
                
                args.Add(new Tuple<string, TokenType>(i1, i2));
            }

            return args;
        }

        private void _EvaluateNode(StmtNode node)
        {
            _diagnostics.SrcFile = node.SrcFile;
            _diagnostics.LineNum = node.LineNum;

            var nodeType = node.Keyword;
            
            if (nodeType == TokenType.PostDecOp)
                _EvaluateDec(node);
            else if (nodeType == TokenType.Del)
                _EvaluateDel(node);
            else if (nodeType == TokenType.Err)
                _EvaluateErr(node);
            else if (nodeType == TokenType.FnCall)
            {
                var fnCallNode = (FnCallNode) node;
                _EvaluateFn(fnCallNode.FnName, fnCallNode.FnArgs);
            }
            else if (nodeType == TokenType.FnResult)
                _EvaluateFnRes(node);
            else if (nodeType == TokenType.For)
            {
                var eval = _EvaluateFor(node);
                while (eval)
                {
                    foreach (var child in node.Body)
                        _EvaluateNode(child);
                    eval = _EvaluateFor(node);
                }

                _currDepth--;
                if (_scopeEnabled)
                    ScopeCleanUp(_currDepth);
            }
            else if (nodeType == TokenType.If)
            {
                if (_EvaluateIf(node))
                    foreach (var child in node.Body)
                        _EvaluateNode(child);
                else
                    foreach (var child in node.Alt)
                        _EvaluateNode(child);

                _currDepth--;
                if (_scopeEnabled)
                    ScopeCleanUp(_currDepth);
            }
            else if (nodeType == TokenType.PostIncOp)
                _EvaluateInc(node);
            else if (nodeType == TokenType.Print)
                _EvaluatePrint(node);
            else if (nodeType == TokenType.Read)
                _EvaluateRead(node);
            else if (nodeType == TokenType.Res)
                _EvaluateRes(node);
            else if (nodeType == TokenType.Return)
                _EvaluateReturn(node);
            else if (nodeType == TokenType.Url)
                _EvaluateUrl(node);
            else if (nodeType == TokenType.Use)
                _EvaluateUse(node);
            else if (nodeType == TokenType.Var)
                _EvaluateVar(node);
            else if (nodeType != TokenType.Def)
                throw new ArgumentException(_diagnostics.InvokeError(node.GetKeyword()));
        }

        protected void EvaluateProgram()
        {
            var body = _fnAddrTable["Main"];
            InitNewFrame();

            foreach (var ch in body.Body)
            {
                _currNode++;
                _EvaluateNode(ch);
            }
            
            PopFrame();

            if (_scopeEnabled)
                ScopeCleanUp(_currDepth);

            if (_urls > _res && _warnsEnabled)
                Console.WriteLine(_diagnostics.MissingResCall());
        }

        protected void SetSourceFile(string srcFile) => _currSrcFile = srcFile;
    }
}