using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using MoonSharp.Interpreter;

namespace ProbablyLua.Api
{
    internal sealed partial class LuaItemActions
    {
        private readonly Dictionary<LuaItemDefinition, List<WeakReference<GameItem>>> _instances = [];
        private readonly ConditionalWeakTable<GameItem, object> _hooked = [];
        private readonly HashSet<string> _filterErrors = new(StringComparer.Ordinal);

        private static LuaActionBinding? Handler(LuaItemDefinition definition,
            string id) => definition.Actions.TryGetValue(id, out var value) ? value : null;

        private static bool HasTargetHandler(LuaItemDefinition definition)
        {
            foreach (var pair in definition.Actions)
                if (pair.Value != null && definition.ActionTypes.Require(pair.Key) !=
                    LuaActionKind.Immediate)
                    return true;

            return false;
        }

        private bool TargetAllowed(LuaItemDefinition definition, GameItem source,
            GameItem target, string id, LuaActionBinding binding, bool useProbeCache = true)
        {
            if (!_committed || target == null || ReferenceEquals(source, target) ||
                LuaItemView.IsDestroyed(source) || LuaItemView.IsDestroyed(target) ||
                !GeneralHelper.IsItemEffectivelyOwned(source))
                return false;

            if (binding.CanTarget == null)
                return true;

            string key = definition.id + "\u0000" + id;
            if (_filterErrors.Contains(key))
                return false;

            if (useProbeCache && LuaTargetProbeScope.TryGet(source, target, binding, out bool cached))
                return cached;

            try
            {
                DynValue result = _callbacks.Inspect(binding.CanTarget,
                    _items.View(source, readOnly: true), _items.View(target, readOnly: true));

                if (result.Type != DataType.Boolean)
                    throw new ScriptRuntimeException(
                        $"Item '{definition.id}' action '{id}': can_target(source, target) " +
                        $"must return true or false, got {result.Type}.");

                if (useProbeCache)
                    LuaTargetProbeScope.Store(source, target, binding, result.Boolean);

                return result.Boolean;
            }
            catch (Exception exception)
            {
                // Highlights poll MayTarget/CanTarget repeatedly. Report once
                // per item/action instead of flooding the game log every frame.
                if (_filterErrors.Add(key))
                    _error($"Target filter '{definition.id}/{id}' failed: {LuaErrorFormatter.Describe(exception)}");

                return false;
            }
        }

        internal void AttachItem(LuaItemDefinition definition, GameItem item)
        {
            if (!_instances.TryGetValue(definition, out var instances))
            {
                instances = new List<WeakReference<GameItem>>();
                _instances.Add(definition, instances);
            }

            if (instances.Count % 128 == 0)
                instances.RemoveAll(reference => !reference.TryGetTarget(out var current) ||
                    (current is GameItemElement element && element.IsDestroyed()));

            instances.Add(new WeakReference<GameItem>(item));
            InstallTargetHandlers(definition, item);
        }

        private void RefreshCreatedItems(LuaItemDefinition definition)
        {
            if (!_instances.TryGetValue(definition, out var instances))
                return;

            for (int i = instances.Count - 1; i >= 0; i--)
            {
                if (instances[i].TryGetTarget(out GameItem? item))
                    InstallTargetHandlers(definition, item);

                else
                    instances.RemoveAt(i);
            }
        }

        private void InstallTargetHandlers(LuaItemDefinition definition, GameItem item)
        {
            // Most content items have no target action. Leave their native
            // delegates untouched; dynamic action/drop registration calls us again.
            if (definition.DropHandler == null && !HasTargetHandler(definition))
                return;

            if (_hooked.TryGetValue(item, out _))
                return;

            _hooked.Add(item, new object());
            var maySelect = item.maySelectSlotItemFunc;
            var canSelect = item.canSelectSlotItemFunc;
            var mayTarget = item.mayThisTargetItemFunc;
            var canTarget = item.canThisTargetItemFunc;
            var onTarget = item.onThisTargetItemFunc;

            item.maySelectSlotItemFunc = (source, inventory, slot) =>
                (_committed && HasTargetHandler(definition)) ||
                (maySelect?.InvokeAllReduce(source, inventory, slot, InvokeFuncExtensions.BoolReduceOR) ?? false);

            item.canSelectSlotItemFunc = (source, inventory, slot) =>
                (_committed && HasTargetHandler(definition)) ||
                (canSelect?.InvokeAllReduce(source, inventory, slot, InvokeFuncExtensions.BoolReduceOR) ?? false);

            item.mayThisTargetItemFunc = (source, target) =>
            {
                LuaActionBinding? binding = TargetHandler(definition, source, out string id);
                return binding == null
                    ? mayTarget?.InvokeAllReduce(source, target, InvokeFuncExtensions.BoolReduceOR) ?? false
                    : TargetAllowed(definition, source, target, id, binding);
            };

            item.canThisTargetItemFunc = (source, target) =>
            {
                LuaActionBinding? binding = TargetHandler(definition, source, out string id);
                return binding == null
                    ? canTarget?.InvokeAllReduce(source, target, InvokeFuncExtensions.BoolReduceOR) ?? false
                    : TargetAllowed(definition, source, target, id, binding);
            };

            item.onThisTargetItemFunc = (source, target) =>
            {
                LuaActionBinding? binding = TargetHandler(definition, source, out string id);
                if (!_committed || binding == null)
                {
                    onTarget?.Invoke(source, target);
                    return;
                }

                // The world may have changed since highlighting. An action must
                // always recheck eligibility and invalidate any surrounding probe.
                LuaTargetProbeScope.Invalidate();
                try
                {
                    if (TargetAllowed(definition, source, target, id, binding, useProbeCache: false))
                        _callbacks.TryInvoke($"Item '{definition.id}', action '{id}'",
                            binding.Callback, _items.View(source), _items.View(target));
                }
                finally { LuaTargetProbeScope.Invalidate(); }
            };
        }
    }
}
