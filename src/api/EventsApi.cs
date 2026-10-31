using System;
using System.Collections.Generic;
using MoonSharp.Interpreter;

namespace ProbablyLua.Api
{
    internal sealed class EventsApi
    {
        private static readonly HashSet<string> KnownEvents = new(StringComparer.Ordinal)
        {
            "game_loaded",
            "new_game_started",
            "generate_customers",
            "customer_trade",
            "loot_ready"
        };

        private readonly Script _script;
        private readonly LuaCallbacks _callbacks;

        private readonly Dictionary<string, List<DynValue>> _handlers = new(
            StringComparer.Ordinal
        );

        public EventsApi(Script script, LuaCallbacks callbacks)
        {
            _script = script;
            _callbacks = callbacks;
        }

        public void Register()
        {
            _script.Globals.Set(
                "on_event",
                DynValue.NewCallback(Subscribe)
            );
        }

        private DynValue Subscribe(ScriptExecutionContext _, CallbackArguments args)
        {
            if (_callbacks.IsReadOnly)
                throw new ScriptRuntimeException(
                    "on_event(): event handlers cannot be registered from a can_target filter."
                );

            if (
                args.Count != 2
                || args[0].Type != DataType.String
                || args[1].Type != DataType.Function
            )
                throw new ScriptRuntimeException(
                    "on_event(name, callback) requires an event name string and a Lua function (example: on_event(\"game_loaded\", function() end))."
                );

            string name = args[0].String;
            if (string.IsNullOrWhiteSpace(name))
                throw new ScriptRuntimeException("on_event(name, callback): event name cannot be empty.");

            if (!KnownEvents.Contains(name))
                throw new ScriptRuntimeException(
                    $"on_event('{name}', ...): unknown event. " +
                    "Available events: game_loaded, new_game_started, generate_customers, customer_trade, loot_ready.");

            if (!_handlers.TryGetValue(name, out var callbacks))
            {
                callbacks = [];
                _handlers.Add(name, callbacks);
            }

            callbacks.Add(args[1]);
            return DynValue.Nil;
        }

        public void Emit(string name, params object[] args)
        {
            if (!_handlers.TryGetValue(name, out var callbacks))
                return;

            foreach (DynValue callback in callbacks.ToArray())
            {
                _callbacks.TryInvoke($"Event '{name}'", callback, args);
            }
        }
    }
}
