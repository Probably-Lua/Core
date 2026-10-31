using System;
using MoonSharp.Interpreter;

namespace ProbablyLua.Api
{
    // Runtime callbacks are synchronous, but never get an unbounded Lua loop.
    // Auto-yield is an instruction budget; it cannot interrupt a native call.
    internal sealed class LuaCallbacks
    {
        private readonly Script _script;
        private readonly Action<string> _error;
        private DynValue? _runnerBody;
        private LuaCallbackRunner? _idleRunner;
        internal bool IsReadOnly { get; private set; }

        internal LuaCallbacks(Script script, Action<string> error)
        {
            _script = script;
            _error = error;
        }

        internal DynValue Invoke(DynValue callback, params object[] args)
        {
                _runnerBody ??= _script.LoadString(LuaCallbackRunner.Source, codeFriendlyName: "[callback runner]");
                LuaCallbackRunner runner = _idleRunner ?? new LuaCallbackRunner(_script, _runnerBody);
                _idleRunner = null;
                DynValue result = runner.Invoke(callback, args);
                _idleRunner ??= runner;
                return result;
        }

        internal DynValue Inspect(DynValue callback, params object[] args)
        {
            bool previous = IsReadOnly;
            IsReadOnly = true;
            try { return Invoke(callback, args); }
            finally { IsReadOnly = previous; }
        }

        internal bool TryInvoke(string operation, DynValue callback, params object[] args)
        {
            try
            {
                Invoke(callback, args);
                return true;
            }
            catch (Exception exception)
            {
                _error(LuaErrorFormatter.Format(operation, exception));
                return false;
            }
        }
    }
}
