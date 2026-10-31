using System;
using System.Collections.Generic;
using MoonSharp.Interpreter;

namespace ProbablyLua.Api
{
    internal sealed partial class LuaItemFactory
    {
        private DynValue Declare(ScriptExecutionContext _, CallbackArguments args)
        {
            if (_sealed)
                throw new ScriptRuntimeException("factory.item() is only available during Lua initialization.");

            if (args.Count < 1 || args.Count > 2 || args[0].Type != DataType.String ||
                (args.Count == 2 && args[1].Type != DataType.String))
                throw new ScriptRuntimeException("factory.item(id, base_item?): expected a local ID and an optional qualified template ID. " +
                    "Example: factory.item('pen', 'base:screwdriver').");

            ResourceId id = ResourceIds.NewItem(_modId, args[0].String);
            if (_items.ContainsKey(id.FullId))
                throw new ScriptRuntimeException($"Item '{id.FullId}' is already defined.");

            var definition = new LuaItemDefinition(_modId, id.FullId, id.LocalId, _actions, _script);
            definition.CheckActionMutation = () =>
            {
                if (_sealed)
                    EnsureMutable();
            };

            if (args.Count == 2)
            {
                ResourceId template = ResourceIds.Parse(args[1].String, _modId);
                // Custom templates can form cycles and capture another runtime's handlers.
                if (!template.IsBase)
                    throw new ScriptRuntimeException($"Item '{id.FullId}': base_item must refer to a vanilla base:<item> template.");

                definition.base_item = template.FullId;
            }
            _items.Add(id.FullId, definition);
            return UserData.Create(definition);
        }

        internal void Bind(Table factory, Script script)
        {
            _script = script;
            LuaUserData.Register<LuaItemDefinition>();
            LuaUserData.Register<LuaItemSounds>();
            LuaUserData.Register<LuaItemContainer>();
            LuaUserData.Register<LuaItemView>();
            LuaUserData.Register<LuaInventoryView>();
            factory.Set("item", DynValue.NewCallback(Declare));

            var items = new Table(script);
            items.Set("spawn", DynValue.NewCallback((_, args) =>
            {
                if (args.Count < 1 || args.Count > 2 || args[0].Type != DataType.String)
                    throw new ScriptRuntimeException("items.spawn(id, amount?): expected an item ID and an optional positive integer.");

                int? amount = args.Count == 1 || LuaValues.IsNil(args[1]) ? null :
                    LuaValues.Integer(args[1], 1, 1000000, "items.spawn amount");

                return UserData.Create(View(Spawn(args[0].String, amount)));
            }));

            items.Set("exists", DynValue.NewCallback((_, args) =>
            {
                if (args.Count != 1 || args[0].Type != DataType.String)
                    throw new ScriptRuntimeException("items.exists(id): expected one item ID string.");

                string id = ResolveItemId(args[0].String);
                return DynValue.NewBoolean(DirectoryMaster.Has<GameItem>(id) || PreBuiltItemHelper.preBuiltItems.ContainsKey(id));
            }));

            items.Set("ids", DynValue.NewCallback((_, args) =>
            {
                if (args.Count != 0)
                    throw new ScriptRuntimeException("items.ids(): no arguments expected.");

                var ids = new SortedSet<string>(DirectoryMaster.GetIdentifierList<GameItem>(), StringComparer.Ordinal);
                ids.UnionWith(PreBuiltItemHelper.preBuiltItems.Keys);
                var result = new Table(script);
                foreach (string id in ids)
                    result.Append(DynValue.NewString(id.Contains(":") ? id : "base:" + id));

                return DynValue.NewTable(result);
            }));

            items.Set("inventory", DynValue.NewCallback((_, args) =>
            {
                if (args.Count != 1 || args[0].Type != DataType.String)
                    throw new ScriptRuntimeException("items.inventory(location): expected a store inventory name.");

                return UserData.Create(StoreInventory(args[0].String));
            }));

            script.Globals.Set("items", DynValue.NewTable(items));
        }

        private LuaInventoryView StoreInventory(string location)
        {
            if (!_active || EmporiumEntry.Instance == null || PlayerStore.Instance == null)
                throw new ScriptRuntimeException("items.inventory(): store inventories are only available during gameplay.");

            var store = EmporiumEntry.Instance;
            GraphNodeStorage? inventory = location switch
            {
                "inventory" => store.invElement,
                "front_counter" => store.frontInvinvElement,
                "back_counter" => store.backInvinvElement,
                "display" => store.showcaseElement,
                "trash" => store.trashInvElement,
                "afterhours" => store.afterhourInventory,
                _ => throw new ScriptRuntimeException($"Unknown inventory '{location}'. Use inventory, front_counter, back_counter, display, trash or afterhours.")
            };

            if (inventory == null)
                throw new ScriptRuntimeException($"Inventory '{location}' is unavailable in the current scene.");

            return Inventory(inventory, readOnly: _callbacks.IsReadOnly);
        }
    }
}
