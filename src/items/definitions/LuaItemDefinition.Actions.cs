using System;
using System.Collections.Generic;
using MoonSharp.Interpreter;

namespace ProbablyLua.Api
{
    public sealed partial class LuaItemDefinition
    {
        private readonly LuaActionCatalog _actionTypes;
        private readonly Dictionary<string, LuaActionBinding?> _actions =
            new(StringComparer.OrdinalIgnoreCase);

        internal IReadOnlyDictionary<string, LuaActionBinding?> Actions => _actions;
        internal LuaActionCatalog ActionTypes => _actionTypes;
        internal Action<string, LuaActionBinding?>? ActionChanged;

        internal LuaActionBinding? DropHandler { get; private set; }
        internal Action? DropChanged;
        internal Action? CheckActionMutation;

        // A separate input handler. It neither creates nor modifies a menu action.
        public void on_drop(DynValue callback, DynValue? can_target = null)
        {
            CheckActionMutation?.Invoke();
            DynValue? handler = LuaValues.OptionalFunction(callback, $"Item '{id}'.on_drop callback");
            DynValue? filter = LuaValues.OptionalFunction(can_target, $"Item '{id}'.on_drop can_target");
            if (handler == null && filter != null)
                throw new ScriptRuntimeException($"Item '{id}'.on_drop: a filter requires a callback.");

            DropHandler = handler == null ? null : new LuaActionBinding(handler, filter);
            DropChanged?.Invoke();
        }

        public void add_action(string id, DynValue callback, DynValue? can_target = null)
        {
            CheckActionMutation?.Invoke();
            string actionId = ResourceIds.ItemActionId(_modId, id);
            LuaActionKind kind = _actionTypes.Require(actionId);
            if (callback == null || callback.Type != DataType.Function)
                throw new ScriptRuntimeException(
                    $"Item '{this.id}', action '{actionId}': add_action() requires a Lua function callback (example: item:add_action(id, function(self) end)).");

            // MoonSharp supplies Void for omitted arguments and Nil for explicit nil.
            if (can_target != null &&
                (can_target.Type == DataType.Nil || can_target.Type == DataType.Void))
                can_target = null;

            if (can_target != null && can_target.Type != DataType.Function)
                throw new ScriptRuntimeException($"Item '{this.id}', action '{actionId}': can_target must be a function or nil, got {can_target.Type}.");

            if (kind == LuaActionKind.Immediate && can_target != null)
                throw new ScriptRuntimeException($"Item '{this.id}', action '{actionId}' is Immediate: a can_target filter is not supported.");

            // Exactly one handler per item/action ID; a repeated call replaces it.
            var binding = new LuaActionBinding(callback, can_target);
            _actions[actionId] = binding;
            ActionChanged?.Invoke(actionId, binding);
        }

        public void remove_action(string id)
        {
            CheckActionMutation?.Invoke();
            string actionId = ResourceIds.ItemActionId(_modId, id);
            _actionTypes.Require(actionId);
            if (ResourceIds.IsVanillaAction(actionId))
                _actions[actionId] = null; // Suppress vanilla action for this item.

            else
                _actions.Remove(actionId);

            ActionChanged?.Invoke(actionId, null);
        }
    }
}
