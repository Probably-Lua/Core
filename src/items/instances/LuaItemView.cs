using System;
using MoonSharp.Interpreter;

namespace ProbablyLua.Api
{
    // A live, deliberately small facade. No GameItem, Unity or CLR objects are
    // exposed to Lua. A reference remains usable until the item is destroyed.
    public sealed class LuaItemView
    {
        private readonly GameItem _item;
        private readonly LuaItemFactory _factory;
        private readonly bool _readOnly;

        internal LuaItemView(GameItem item, LuaItemFactory factory,
            bool readOnly = false)
        {
            _item = item;
            _factory = factory;
            _readOnly = readOnly;
        }

        public string id => _item.identifier.Contains(":") ? _item.identifier : "base:" + _item.identifier;
        public bool destroyed => IsDestroyed(_item);
        public int unique_id => Require().uniqueId;
        public string name => Require().name;
        public int amount => Require().unitCount;
        public long value => Require().unitValue;
        public bool owned => GeneralHelper.IsItemEffectivelyOwned(Require());
        public bool locked => LuaItemState.IsLocked(Require());
        public bool has_container => Require().contentWindow != null;
        public string description => Require().shortDescription;
        public string long_description => Require().longDescription;
        public string flavor => Require().flavorText;
        public bool stolen => StolenHelper.IsStolenItem(Require());
        public int weight => GeneralHelper.GetWeight(Require());
        public int durability => DurabilityHelper.GetRealDurability(Require());
        public int max_durability => DurabilityHelper.GetMaxDurability(Require());

        public bool has_type(string type) => Require().GetGameItemType().Contains(ItemTypeApi.Require(type));
        public bool has_tag(string key) => Require().IsTag(LuaValues.Text(key, "Tag ID"));
        public string tag_string(string key) => ReadTag(key).GetString();
        public double tag_number(string key) => ReadTag(key).GetDouble();
        public int tag_int(string key) => ReadTag(key).GetInt();
        public bool tag_bool(string key) => ReadTag(key).GetBool();

        private TagState ReadTag(string key)
        {
            LuaValues.Text(key, "Tag ID");
            return Require().modifiedState.dict.TryGetValue(key, out TagState tag) ? tag : new TagState(key);
        }

        public void set_tag(string key, DynValue value) => LuaItemTags.Apply(Mutable(), key, value);
        public void remove_tag(string key) => Mutable().DisableTag(LuaValues.Text(key, "Tag ID"));
        public void add_type(string type) => Mutable().SetGameItemType(ItemTypeApi.Require(type));
        public void remove_type(string type) => Mutable().RemoveGameItemType(ItemTypeApi.Require(type));
        // MoonSharp maps Lua set_name/set_value/set_amount to PascalCase methods.
        // C# reserves those snake_case names for property accessors, even when read-only.
        public void SetName(string value) => Mutable().name = LuaValues.Text(value, "Item name");
        public void set_sprite(string key) => _factory.Assets.SetSprite(Mutable(), key);

        public void SetValue(long value)
        {
            if (value < 0 || value > 9007199254740991L)
                throw new ScriptRuntimeException("set_value(value): expected an integer from 0 to 2^53 - 1.");
            Mutable().SetValue(value);
        }

        public void SetAmount(double amount) => LuaItemState.SetAmount(Mutable(), LuaValues.Integer(amount, 1, 1000000, "Item amount"));
        public void set_contraband(string level) => ContrabandLevelApi.Apply(Mutable(), ContrabandLevelApi.Require(level));
        public void SetDescription(string text) => Mutable().shortDescription = text ?? "";
        public void SetLongDescription(string text) => Mutable().longDescription = text ?? "";
        public void SetFlavor(string text) => Mutable().flavorText = text ?? "";
        public void SetOwned(bool value) => GeneralHelper.SetItemOwned(Mutable(), value);
        public void SetWeight(int value) => GeneralHelper.SetItemWeight(Mutable(), LuaValues.Range(value, 0, 1000000, "Item weight"));

        public void SetStolen(bool value)
        {
            GameItem item = Mutable();
            if (value)
                StolenHelper.InitStolenItem(item);

            else
                StolenHelper.RemoveStolenStatus(item);
        }

        public void SetDurability(int value)
        {
            GameItem item = Mutable();
            if (!item.IsTag("DURABILITY_TAG"))
                throw new ScriptRuntimeException($"Item '{id}' has no durability. Set definition.durability or use a template with durability.");

            DurabilityHelper.SetCurrentDurability(item, LuaValues.Range(value, 0, DurabilityHelper.GetMaxDurability(item), "Item durability"));
        }

        public Table types()
        {
            var result = new Table(_factory.Script);
            foreach (string type in Require().GetGameItemType())
                result.Append(DynValue.NewString(type));

            return result;
        }

        public Table tag_keys()
        {
            var result = new Table(_factory.Script);
            foreach (var pair in Require().state.dict)
                if (pair.Value.IsEnabled())
                    result.Append(DynValue.NewString(pair.Key));

            return result;
        }

        public LuaInventoryView? container()
        {
            GameItem item = Require();
            return item.contentWindow?.child == null ? null : _factory.Inventory(item.contentWindow.child, item, _readOnly);
        }

        public LuaInventoryView? inventory()
        {
            GameInventory? inventory = Require().parentInventory;
            return inventory == null ? null : _factory.Inventory(inventory, readOnly: _readOnly);
        }

        public LuaItemView? parent()
        {
            GameInventory? inventory = Require().parentInventory;
            if (inventory == null)
                return null;

            foreach (GraphNodeStorage ancestor in LuaInventoryGraph.Ancestors(inventory))
                if (ancestor is GameItem item)
                    return _factory.View(item, _readOnly);

            return null;
        }

        public Table contents(bool recursive = false) => container()?.items(recursive) ?? new Table(_factory.Script);

        public int move_to(LuaInventoryView destination, double? amount = null)
        {
            if (destination == null)
                throw new ScriptRuntimeException("item:move_to(inventory, amount?): expected an inventory instance.");

            return destination.insert(this, amount);
        }

        public LuaItemView split(double amount)
        {
            GameItem item = Removable();
            int requested = LuaValues.Integer(amount, 1, item.unitCount - 1, "item:split amount");
            if (requested > item.MaxNumRemove())
                throw new ScriptRuntimeException("item:split(): the native inventory rules do not allow removing this amount.");

            return _factory.View(LuaItemState.Split(item, requested));
        }

        public bool consume(double amount = 1)
        {
            GameItem item = Mutable();
            if (!can_consume(amount))
                return false;

            int requested = (int)amount;
            if (requested == item.unitCount)
                DestroyTree(item);

            else
                LuaItemState.SetAmount(item, item.unitCount - requested);

            return true;
        }

        public bool can_consume(double amount = 1)
        {
            int requested = LuaValues.Integer(amount, 1, 1000000, "item:can_consume amount");
            if (destroyed || requested > _item.unitCount || requested > _item.MaxNumRemove())
                return false;

            if (_item.parentInventory == null)
                return true;

            try
            {
                LuaInventoryGraph.CheckAccess(_item.parentInventory, _factory, inserting: false);
                return _item.MayRemove();
            }
            catch (ScriptRuntimeException) { return false; }
        }

        public bool unlock()
        {
            GameItem item = Mutable();
            if (!LockHelper.IsCurrentlyLocked(item))
                return false;

            LockHelper.UnlockContainer(item);
            EmporiumAudioPlayer.Instance?.audioClickClick?.Play();
            return true;
        }

        public void destroy() => DestroyTree(Removable());

        public bool add_content(string itemId)
        {
            Mutable();
            LuaInventoryView? inventory = container();
            if (inventory == null)
                throw new ScriptRuntimeException($"Item '{id}' has no inventory. Configure item.container.width and height first.");

            return inventory.AddContent(itemId);
        }

        public bool play_sound(string key)
        {
            Mutable();
            string? sound = _factory.Assets.Sound(key);
            return sound != null && AudioManager.Instance != null && AudioManager.Instance.Play(sound);
        }

        internal GameItem Require()
        {
            if (destroyed)
                throw new ScriptRuntimeException($"Item '{id}' has been destroyed; it can no longer be used.");

            return _item;
        }

        internal GameItem Mutable()
        {
            if (_readOnly)
                throw new ScriptRuntimeException("can_target callbacks may only inspect items; perform changes in the action callback.");

            _factory.EnsureMutable();
            _factory.ChargeOperation();
            return Require();
        }

        private GameItem Removable()
        {
            GameItem item = Mutable();
            if (item.parentInventory != null)
                LuaInventoryGraph.CheckAccess(item.parentInventory, _factory, inserting: false);

            if (item.parentInventory != null && !item.MayRemove())
                throw new ScriptRuntimeException($"Item '{id}' cannot be removed from its current inventory.");

            return item;
        }

        internal static bool IsDestroyed(GameItem item) => item is GameItemElement element && element.IsDestroyed();

        internal static void DestroyTree(GameItem item)
        {
            foreach (GameItem child in GraphUtils.FindAllChildrenType<GameItem>(item))
                if (!ReferenceEquals(child, item))
                    child.Destroy();

            item.Destroy();
        }
    }
}
