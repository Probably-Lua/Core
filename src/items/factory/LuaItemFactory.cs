using System;
using System.Collections.Generic;
using MoonSharp.Interpreter;

namespace ProbablyLua.Api
{
    internal sealed partial class LuaItemFactory
    {
        private readonly string _modId;
        private readonly string _modName;
        private readonly LangApi _lang;
        private readonly LuaActionCatalog _actions;
        private readonly LuaCallbacks _callbacks;
        private readonly Action<string> _warning;
        private readonly LuaItemAssets _assets;
        private readonly Dictionary<string, LuaItemDefinition> _items = new(StringComparer.Ordinal);
        private readonly HashSet<GameItem> _filling = new();
        private readonly HashSet<string> _callbackErrors = new(StringComparer.Ordinal);
        private bool _sealed;
        private bool _active;
        private int _spawnDepth;
        private int _pendingNotifications;
        private Script _script = null!;

        internal IEnumerable<LuaItemDefinition> Definitions => _items.Values;
        internal Action<LuaItemDefinition, GameItem>? OnItemCreated;
        internal LuaItemAssets Assets => _assets;
        internal Script Script => _script;

        internal LuaItemFactory(string modId, string modName, LangApi lang, LuaActionCatalog actions, LuaCallbacks callbacks, Action<string> warning)
        {
            _modId = modId;
            _modName = modName;
            _lang = lang;
            _actions = actions;
            _callbacks = callbacks;
            _warning = warning;
            _assets = new LuaItemAssets(modId);
        }

        internal void Seal()
        {
            foreach (LuaItemDefinition definition in _items.Values)
            {
                try
                {
                    definition.Validate();
                    _assets.Validate(definition);
                    foreach (object manufacturer in Manufacturers(definition))
                        ManufacturerTag(manufacturer);
                }
                catch (Exception exception)
                {
                    throw new ScriptRuntimeException($"Item '{definition.id}': {LuaErrorFormatter.Describe(exception)}");
                }

                if (string.IsNullOrEmpty(definition.sprite) && definition.base_item == null)
                    _warning($"Item '{definition.id}' has no sprite; the game will display its placeholder. Set item.sprite to a Textures/Items/ filename without extension.");
            }
            _sealed = true;
            _active = true;
        }

        internal void RegisterItems(ModItemDirectory directory)
        {
            foreach (LuaItemDefinition definition in _items.Values)
            {
                if (!directory.Add(definition.id, () => CreateItem(definition)))
                    throw new InvalidOperationException($"Item '{definition.id}' could not be added to the game directory.");
            }
        }

        internal bool CanFill(GameItem item) => _active && !_callbacks.IsReadOnly && _filling.Contains(item);
        internal LuaInventoryView Inventory(GraphNodeStorage root, GameItem? owner = null, bool readOnly = false) =>
            new(root, this, owner, readOnly);

        internal LuaItemView View(GameItem item, bool readOnly = false) => new(item, this, readOnly);

        internal void EnsureMutable()
        {
            if (!_active)
                throw new ScriptRuntimeException("The Lua mod is not active; item operations are unavailable.");

            if (_callbacks.IsReadOnly)
                throw new ScriptRuntimeException("can_target callbacks may only inspect items; perform changes in the action callback.");
        }

        internal void Clear()
        {
            _active = false;
            _filling.Clear();
            _callbackErrors.Clear();
        }
    }
}
