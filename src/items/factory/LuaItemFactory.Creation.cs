using System;
using System.Collections.Generic;
using MoonSharp.Interpreter;

namespace ProbablyLua.Api
{
    internal sealed partial class LuaItemFactory
    {
        internal string ResolveItemId(string input)
        {
            ResourceId id = ResourceIds.Parse(input, _modId);
            return id.IsBase ? id.LocalId : id.FullId;
        }

        internal GameItem Spawn(string input, int? amount = null)
        {
            EnsureMutable();
            _callbacks.ChargeItemOperation();
            if (!_active || PlayerStore.Instance == null)
                throw new ScriptRuntimeException("items.spawn(): gameplay is not ready. Spawn items from a gameplay event or an item action.");

            if (_spawnDepth >= 16)
                throw new ScriptRuntimeException("Item spawning exceeded 16 nested calls. Check recursive container contents.");

            string id = ResolveItemId(input);
            if (!DirectoryMaster.Has<GameItem>(id) && !PreBuiltItemHelper.preBuiltItems.ContainsKey(id))
                throw new ScriptRuntimeException($"Item '{id}' is not registered in this game. Use base:<id> for vanilla items.");

            _spawnDepth++;
            try
            {
                GameItem item = ItemSpawner.Spawn(id) ?? throw new ScriptRuntimeException($"Item '{id}' could not be created.");
                try
                {
                    if (amount.HasValue)
                        LuaItemState.SetAmount(item, amount.Value);

                    return item;
                }
                catch
                {
                    LuaItemView.DestroyTree(item);
                    throw;
                }
            }
            finally { _spawnDepth--; }
        }

        private GameItem CreateItem(LuaItemDefinition definition)
        {
            definition.Validate();
            if (definition.base_item != null && !DirectoryMaster.Has<GameItem>(ResolveItemId(definition.base_item)))
                throw new ScriptRuntimeException($"Item '{definition.id}': template '{definition.base_item}' is not registered in this game. Use items.ids() to find available IDs.");

            GameItem item = definition.base_item == null
                ? ItemDirectory.CreateEmptyItem(definition.id)
                : DirectoryMaster.Item(ResolveItemId(definition.base_item));

            if (item == null)
                throw new ScriptRuntimeException($"Item '{definition.id}': template '{definition.base_item}' was not found in the current game.");

            try
            {
                item.identifier = definition.id;
                item.name = Text(definition, "name", definition.name, item.name, definition.id);
                item.shortDescription = Text(definition, "description", definition.description, item.shortDescription);
                item.longDescription = Text(definition, "long_description", definition.long_description, item.longDescription);
                item.flavorText = Text(definition, "flavor", definition.flavor, item.flavorText);
                definition.ApplyProperties(item);
                if (definition.base_item == null || definition.HasContrabandLevel)
                    ContrabandLevelApi.Apply(item, definition.ContrabandLevel);

                ApplyManufacturers(definition, item);
                _assets.Apply(definition, item);
                ApplyContainer(definition, item);
                if (definition.nicotine > 0)
                    ConsumableHelper.InitNicotine(item, definition.nicotine);

                definition.ApplyMechanics(item);
                AttachLifecycle(definition, item);
                ModHelper.SetSource(item, _modId, _modName);
                OnItemCreated?.Invoke(definition, item);
                LuaItemLifecycle.Track(item, this, definition);
                return item;
            }
            catch
            {
                LuaItemView.DestroyTree(item);
                throw;
            }
        }

        private string Text(LuaItemDefinition item, string field, string value, string native, string fallback = "") =>
            _lang.Resolve(ResourceIds.TranslationPath("item", item.LocalId, field),
                !string.IsNullOrEmpty(value) ? value : !string.IsNullOrEmpty(native) ? native : fallback);

        private static IEnumerable<object> Manufacturers(LuaItemDefinition definition)
        {
            if (definition.manufacturer != null)
                yield return definition.manufacturer;

            foreach (object entry in definition.AdditionalManufacturers)
                yield return entry;
        }

        private string ManufacturerTag(object value) => ResourceIds.ToManufacturerTag(_modId,
            value is LuaManufacturer manufacturer ? manufacturer.id : (string)value);

        private void ApplyManufacturers(LuaItemDefinition definition, GameItem item)
        {
            var tags = new List<string>();
            foreach (object value in Manufacturers(definition))
            {
                string tag = ManufacturerTag(value);
                if (!tags.Contains(tag))
                    tags.Add(tag);
            }

            if (tags.Count == 0)
                return;

            item.EnableManufacturer(tags[0]);
            if (tags.Count > 1)
                item.ModifyTag(ManufacturerRegistry.AdditionalTag,
                    state => state.Enable().SetString(string.Join("\n", tags.GetRange(1, tags.Count - 1))));
        }

        private void ApplyContainer(LuaItemDefinition definition, GameItem item)
        {
            LuaItemContainer options = definition.container;
            if (!options.Enabled)
                return;

            if (item.contentWindow != null)
                throw new ScriptRuntimeException($"Item '{definition.id}': its template already has a container; do not specify another one.");

            var (window, inventory) = DirectoryUtils.CreateInventoryWindow(options.width, options.height);
            item.SetContentWindow(window);
            item.EnableTag("CONTAINER_TAG");
            if (!string.IsNullOrEmpty(options.lock_id))
            {
                LockHelper.InitLockedContainerItem(item, options.lock_id, inventory);
                LockHelper.LockUpContainer(item);
            }

            if (!string.IsNullOrEmpty(options.opened_sprite))
                item.onUnlockedFunc += unlocked => _assets.SetSprite(unlocked, options.opened_sprite);
        }
    }
}
