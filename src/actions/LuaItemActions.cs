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
        private readonly LuaCallbacks _callbacks;
        private readonly LuaItemFactory _items;
        private readonly LangApi _lang;
        private readonly LuaActionCatalog _types;
        private readonly Action<string> _error;
        private readonly Dictionary<string, ContextAction> _registered = new(StringComparer.OrdinalIgnoreCase);
        private bool _committed;

        internal LuaItemActions(Script script, LuaItemFactory items, LangApi lang, LuaActionCatalog types, LuaCallbacks callbacks, Action<string> error)
        {
            _callbacks = callbacks;
            _items = items;
            _lang = lang;
            _types = types;
            _error = error;
            LuaUserData.Register<LuaItemView>();
        }

        internal void Bind(Script script)
        {
            var constants = new Table(script);
            foreach (string id in ResourceIds.VanillaActionIds)
            {
                string shortId = id.Substring(ResourceIds.BaseModId.Length + 1);
                string name = char.ToUpperInvariant(shortId[0]) + shortId.Substring(1);
                constants.Set(name, DynValue.NewString(id));
            }

            script.Globals.Set("ItemAction", DynValue.NewTable(constants));
        }

        internal void Commit()
        {
            if (_committed)
                return;

            _committed = true;
            foreach (LuaItemDefinition definition in _items.Definitions)
            {
                definition.DropChanged = () =>
                {
                    _filterErrors.Remove(definition.id + "\u0000on_drop");
                    LuaTargetProbeScope.Invalidate();
                    RefreshCreatedItems(definition);
                };
                definition.ActionChanged = (id, binding) =>
                {
                    LuaTargetProbeScope.Invalidate();
                    Apply(definition, id, binding);
                    RefreshCreatedItems(definition);
                };

                foreach (var pair in definition.Actions)
                    Apply(definition, pair.Key, pair.Value);
            }
        }

        private void Apply(LuaItemDefinition definition, string id,
            LuaActionBinding? binding)
        {
            string key = definition.id + "\u0000" + id;
            _filterErrors.Remove(key);
            if (_registered.TryGetValue(key, out ContextAction? old))
            {
                ContextActions.UnregisterIfSame(old);
                _registered.Remove(key);
            }

            if (binding == null && !ResourceIds.IsVanillaAction(id))
                return;

            ContextAction action;
            if (binding == null)
            {
                action = new ContextAction
                {
                    ItemId = definition.id,
                    Id = id,
                    IsSuppressed = true
                };
            }
            else
            {
                string nameKey = ResourceIds.IsVanillaAction(id)
                    ? ResourceIds.TranslationPath("action",
                        ResourceIds.ActionLabelFallback(id), "name")
                    : ResourceIds.ActionNameKey(id);

                string fallback = ResourceIds.ActionLabelFallback(id);
                LuaActionKind kind = _types.Require(id);
                action = new ContextAction
                {
                    ItemId = definition.id,

                    Id = id,

                    Label = _ => _lang.Resolve(nameKey, fallback,
                        ResourceIds.IsVanillaAction(id) ? "Mechanic" : null,
                        ResourceIds.IsVanillaAction(id) ? "handler_btn_" + fallback : null),

                    CanExecute = item => _committed && GeneralHelper.IsItemEffectivelyOwned(item),

                    Execute = item =>
                    {
                        if (kind == LuaActionKind.Immediate)
                        {
                            try
                            {
                                _callbacks.Invoke(binding.Callback, _items.View(item));
                            }
                            catch (Exception exception)
                            {
                                _error($"Item '{definition.id}' action '{id}' callback failed: {LuaErrorFormatter.Describe(exception)}");
                            }
                        }
                        else
                        {
                            StartSelection(item, id, kind);
                        }
                    }
                };
            }

            try
            {
                ContextActions.Register(action);
                _registered[key] = action;
            }
            catch (Exception exception)
            {
                _error($"Failed to register item action '{id}': {LuaErrorFormatter.Describe(exception)}");
            }
        }

        internal void Clear()
        {
            LuaTargetProbeScope.Invalidate();
            foreach (LuaItemDefinition definition in _items.Definitions)
            {
                definition.ActionChanged = null;
                definition.DropChanged = null;
            }

            foreach (ContextAction action in _registered.Values)
                ContextActions.UnregisterIfSame(action);

            _registered.Clear();
            _instances.Clear();
            _filterErrors.Clear();

            if (ReferenceEquals(_active?.Owner, this))
                _active = null;

            if (ReferenceEquals(_pending?.Owner, this))
                _pending = null;

            _committed = false;
        }
    }
}
