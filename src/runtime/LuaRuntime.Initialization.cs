using System;
using System.Collections.Generic;
using System.Diagnostics;
using MoonSharp.Interpreter;
using ProbablyLua.Api;

namespace ProbablyLua
{
    internal sealed partial class LuaRuntime
    {
        private static readonly TimeSpan InitLimit = TimeSpan.FromSeconds(10);
        private static readonly TimeSpan SliceLimit = TimeSpan.FromMilliseconds(2);
        private readonly Stopwatch _initTime = new();

        private DynValue? _initializer;

        public void BeginLoad(string source)
        {
            _initTime.Start();

            try
            {
                DynValue chunk = _script.LoadString(
                    source,
                    codeFriendlyName: LuaScriptPaths.Entry
                );

                _initializer = _script.CreateCoroutine(chunk);
                _initializer.Coroutine.AutoYieldCounter = 1000;
            }
            finally
            {
                _initTime.Stop();
            }

            CheckTimeout();
        }

        public bool Step()
        {
            if (_initializer == null)
                throw new InvalidOperationException("Lua initialization was not started.");

            CheckTimeout();

            bool completed = false;
            var slice = Stopwatch.StartNew();

            _initTime.Start();

            try
            {
                do
                {
                    DynValue result = _initializer.Coroutine.Resume();

                    if (result.Type != DataType.YieldRequest)
                    {
                        if (_initializer.Coroutine.State != CoroutineState.Dead)
                            throw new ScriptRuntimeException("Unexpected yield during Lua initialization.");

                        completed = true;
                        break;
                    }
                }
                while (slice.Elapsed < SliceLimit && _initTime.Elapsed < InitLimit);
            }
            finally
            {
                _initTime.Stop();
            }

            CheckTimeout();

            if (completed)
                _initializer = null;

            return completed;
        }

        private void CheckTimeout()
        {
            if (_initTime.Elapsed >= InitLimit)
                throw new TimeoutException($"{LuaScriptPaths.Entry} exceeded 10 seconds of execution time.");
        }
    }
}
