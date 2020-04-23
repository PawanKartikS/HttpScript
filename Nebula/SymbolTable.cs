using System;
using System.Net;
using Nebula.SyntaxNodes;
using System.Collections.Generic;

namespace Nebula
{
    internal class Decl
    {
        public readonly string Value;
        private readonly string _fnScope;
        public readonly Tokens.TokenType Type;

        public readonly int Depth;
        public readonly bool IsConstant;
        public readonly HttpStatusCode StatusCode;

        public Decl(string value, Tokens.TokenType type, int depth, string fnScope, bool isConstant,
            HttpStatusCode statusCode)
        {
            Value = value;
            _fnScope = fnScope;
            Type = type;
            Depth = depth;
            IsConstant = isConstant;
            StatusCode = statusCode;
        }
    }

    internal class SymbolTable
    {
        // Each function is allotted a frame in which symbols are stored. Every time a function is invoked
        // a new frame is pushed to the stack. This allows us to declare variables with same name but in
        // different functions.
        //
        // When the controller finishes executing a function, PopFrame() is called to discard the top frame
        // thereby restoring previous function's symbol states.
        private readonly Stack<Dictionary<string, Decl>> _table;
        private readonly Dictionary<string, UrlNode> _apiRequests;

        // Maps a function name to it's arguments. A function is invoked only if the required number of args
        // are provided/available.
        private readonly Dictionary<string, List<string>> _fnSignatures;
        private readonly Stack<Tuple<string, int>> _scopeResolution;

        protected SymbolTable()
        {
            _table = new Stack<Dictionary<string, Decl>>();
            _apiRequests = new Dictionary<string, UrlNode>();
            _fnSignatures = new Dictionary<string, List<string>>();
            _scopeResolution = new Stack<Tuple<string, int>>();
        }

        protected void DumpSymbolTable()
        {
            foreach (var (key, value) in _apiRequests)
                Console.WriteLine($"entry: {key}");

            foreach (var (key, value) in _table.Peek())
                Console.WriteLine($"entry: {key}, {value}");
        }

        protected List<string> GetFnSignature(string fnName) => _fnSignatures[fnName];

        protected Decl GetSymbolValue(string symbol)
        {
            if (_apiRequests.ContainsKey(symbol))
                throw new ArgumentException($"fatal: {symbol} is an object; unsupported action");
            return _table.Peek()[symbol];
        }

        protected UrlNode GetUrlNode(string apiIdf)
        {
            if (!_apiRequests.ContainsKey(apiIdf))
                throw new ArgumentException($"fatal: no registered endpoint with reference to {apiIdf}");
            return _apiRequests[apiIdf];
        }

        protected void InitNewFrame() => _table.Push(new Dictionary<string, Decl>());

        protected bool IsRequestRegistered(string apiIdf, bool delRequest = false)
        {
            var ret = _apiRequests.ContainsKey(apiIdf);
            if (ret && delRequest)
                _apiRequests.Remove(apiIdf);
            return ret;
        }

        protected bool IsSymbolRegistered(string symbol, bool delSymbol = false)
        {
            var ret = _table.Peek().ContainsKey(symbol);
            if (ret && delSymbol)
                _table.Peek().Remove(symbol);
            return ret;
        }

        protected void PopFrame() => _table.Pop();

        protected void RegisterRequest(string apiIdf, UrlNode node)
        {
            if (_apiRequests.ContainsKey(apiIdf))
                throw new ArgumentException($"fatal: existing reference to an endpoint through {apiIdf}");
            _apiRequests[apiIdf] = node;
        }

        protected void RegisterFnSignature(string fnName, List<string> args)
        {
            if (_fnSignatures.ContainsKey(fnName))
                throw new ArgumentException($"fatal: function signature already exists for {fnName}");
            _fnSignatures[fnName] = args;
        }

        protected void RegisterSymbol(string symbol, string value, Tokens.TokenType type, int depthLevel,
            string fnScope, bool isConstant = false, HttpStatusCode statusCode = HttpStatusCode.Unused)
        {
            if (_table.Count == 0)
                throw new ArgumentException("fatal: no frames available in _table; call InitNewFrame().");

            if (_table.Peek().ContainsKey(symbol))
                if (_table.Peek()[symbol].IsConstant)
                    throw new ArgumentException($"fatal: symbol {symbol} exists and marked constant");

            // var f = ...                [Depth 0]
            // if <some_condition>
            //     var f = ...            [Depth 1]
            // end
            //
            // Set the depth to appropriate value to prevent the variable from getting cleared by ScopeCleanUp(). 
            if (_table.Peek().ContainsKey(symbol))
                depthLevel = _table.Peek()[symbol].Depth;

            _table.Peek()[symbol] = new Decl(value, type, depthLevel, fnScope, isConstant, statusCode);
            _scopeResolution.Push(new Tuple<string, int>(symbol, depthLevel));
        }

        protected void ScopeCleanUp(int currentDepth)
        {
            // Clean up variables only in the present frame as we discard the entire frame
            // when controller calls PopFrame().
            if (_scopeResolution.Count == 0)
                return;

            while (_scopeResolution.Peek().Item2 > currentDepth)
            {
                _table.Peek().Remove(_scopeResolution.Peek().Item1);
                _scopeResolution.Pop();
            }
        }
    }
}