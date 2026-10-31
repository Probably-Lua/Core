using System;
using System.Collections.Generic;
using MoonSharp.Interpreter;

namespace ProbablyLua.Api
{
    // Identifies the *mechanism* of an action, not a handler for a specific item.
    // Every item:add_action() resolves its ID here, at definition time.
    internal sealed class LuaActionCatalog
    {
        private readonly string _modId;
        private readonly Dictionary<string, LuaActionKind> _types =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ["base:open"] = LuaActionKind.Immediate,
                ["base:use"] = LuaActionKind.Target,
                ["base:equip"] = LuaActionKind.Equip,
                ["base:activate"] = LuaActionKind.Immediate,
                ["base:toggle"] = LuaActionKind.Immediate,
                ["base:unload"] = LuaActionKind.Immediate,
                ["base:pin"] = LuaActionKind.Immediate
            };
        private bool _sealed;

        internal LuaActionCatalog(string modId) => _modId = modId;

        internal void Bind(Table factory, Script script)
        {
            var constants = new Table(script);
            constants.Set("Immediate", DynValue.NewString("immediate"));
            constants.Set("Target", DynValue.NewString("target"));
            constants.Set("Equip", DynValue.NewString("equip"));
            script.Globals.Set("ActionType", DynValue.NewTable(constants));
            factory.Set("action", DynValue.NewCallback(Declare));
        }

        private DynValue Declare(ScriptExecutionContext _, CallbackArguments args)
        {
            if (_sealed)
                throw new ScriptRuntimeException("factory.action() is only available during Lua initialization.");

            if (args.Count != 2 || args[0].Type != DataType.String ||
                args[1].Type != DataType.String)
                throw new ScriptRuntimeException("factory.action(id, type) requires an action ID string and ActionType.Immediate, ActionType.Target or ActionType.Equip.");

            string id = ResourceIds.ItemActionId(_modId, args[0].String);
            if (ResourceIds.IsVanillaAction(id))
                throw new ScriptRuntimeException($"Vanilla action '{id}' already has a predefined type.");

            if (!id.StartsWith(_modId + ":", StringComparison.OrdinalIgnoreCase))
                throw new ScriptRuntimeException("factory.action() can only declare actions belonging to this mod.");

            LuaActionKind kind;
            switch (args[1].String)
            {
                case "immediate": kind = LuaActionKind.Immediate; break;
                case "target": kind = LuaActionKind.Target; break;
                case "equip": kind = LuaActionKind.Equip; break;
                default:
                    throw new ScriptRuntimeException(
                        $"factory.action('{id}', ...): unknown action type '{args[1].String}'. " +
                        "Use ActionType.Immediate, ActionType.Target or ActionType.Equip.");
            }

            if (_types.ContainsKey(id))
                throw new ScriptRuntimeException($"Action '{id}' is already declared.");

            _types.Add(id, kind);
            return DynValue.Nil;
        }

        internal LuaActionKind Require(string id)
        {
            if (!_types.TryGetValue(id, out LuaActionKind kind))
                throw new ScriptRuntimeException(
                    $"Unknown action '{id}'. Custom actions must be declared with " +
                    "factory.action(id, ActionType.Immediate/Target/Equip) before item:add_action().");

            return kind;
        }

        internal void Seal() => _sealed = true;
    }
}
