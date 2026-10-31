using System;
using MoonSharp.Interpreter;

namespace ProbablyLua.Api
{
    // MoonSharp allocates large VM stacks for each new coroutine. Keep a private
    // worker suspended between completed calls instead.
    internal sealed class LuaCallbackRunner
    {
        internal const string Source = @"
local take, finish = ...
local function invoke(callback, ...)
    return callback(...)
end
while true do
    finish(invoke(take()))
end";

        private readonly Script _script;
        private readonly Coroutine _coroutine;
        private readonly DynValue _take;
        private readonly DynValue _finish;
        private readonly DynValue _completedToken;
        private DynValue[]? _arguments;
        private DynValue _result = DynValue.Void;
        private bool _started;
        private bool _completed;

        internal LuaCallbackRunner(Script script, DynValue body)
        {
            _script = script;
            _coroutine = script.CreateCoroutine(body).Coroutine;
            _completedToken = DynValue.NewTable(script);
            _take = DynValue.NewCallback((_, _) =>
            {
                DynValue arguments = DynValue.NewTuple(_arguments!);
                _arguments = null;
                return arguments;
            });

            _finish = DynValue.NewCallback((_, results) =>
            {
                _result = results.Count == 0 ? DynValue.Void : DynValue.NewTuple(results.GetArray());
                _completed = true;
                return DynValue.NewYieldReq(new[] { _completedToken });
            });
        }

        internal DynValue Invoke(DynValue callback, object[] args)
        {
            _completed = false;
            _arguments = new DynValue[args.Length + 1];
            _arguments[0] = callback;

            try
            {
                for (int i = 0; i < args.Length; i++)
                    _arguments[i + 1] = DynValue.FromObject(_script, args[i]);

                DynValue yielded = _started
                    ? _coroutine.Resume()
                    : _coroutine.Resume(_take, _finish);

                _started = true;

                if (!_completed ||
                    _coroutine.State != CoroutineState.Suspended ||
                    yielded.Type != DataType.Table ||
                    !ReferenceEquals(yielded.Table, _completedToken.Table))
                {
                    throw new ScriptRuntimeException(
                        "Lua callback yielded unexpectedly."
                    );
                }

                return _result;
            }
            finally
            {
                _arguments = null;
                _result = DynValue.Void;
            }
        }
    }
}
