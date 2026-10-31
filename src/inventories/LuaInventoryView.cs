using System;
using MoonSharp.Interpreter;

namespace ProbablyLua.Api
{
    // All native graph references stay private. Lua receives detached tables of
    // item facades and explicit operations, never an inventory or Unity object.
    public sealed class LuaInventoryView
    {
        private readonly GraphNodeStorage _root;
        private readonly LuaItemFactory _factory;
        private readonly GameItem? _owner;
        private readonly bool _readOnly;

        internal LuaInventoryView(GraphNodeStorage root, LuaItemFactory factory, GameItem? owner, bool readOnly)
        {
            _root = root;
            _factory = factory;
            _owner = owner;
            _readOnly = readOnly;
        }

        public Table items(bool recursive = false)
        {
            Require();
            var result = new Table(_factory.Script);
            foreach (GameItem item in LuaInventoryGraph.Items(_root, recursive))
                result.Append(UserData.Create(_factory.View(item, _readOnly)));

            return result;
        }

        public long count(string? id = null, bool recursive = false)
        {
            Require();
            string? identifier = id == null ? null : _factory.ResolveItemId(id);
            long result = 0;
            foreach (GameItem item in LuaInventoryGraph.Items(_root, recursive))
                if (identifier == null || item.identifier == identifier)
                    result += item.unitCount;

            return result;
        }

        public bool can_accept(LuaItemView item)
        {
            Require();
            if (item == null)
                throw new ScriptRuntimeException("inventory:can_accept(item): expected an item instance.");

            GameItem native = item.Require();
            if (ReferenceEquals(native.parentInventory, _root) ||
                (native.parentInventory != null && !native.MayRemove()))
                return false;

            try
            {
                CheckTransfer(native);
                using var fill = FillScope(native);
                SlotMarker? slot = _root.TryFindOneValidInventorySlot(native);
                return slot != null && slot.numTransfer > 0 && native.MaxNumRemove() > 0;
            }
            catch (ScriptRuntimeException)
            {
                // A capacity/rule query is false for locked inventories or cycles.
                return false;
            }
        }

        // Return the number of units actually moved. A refused or partial move
        // leaves the original item and its remaining units with the caller.
        public int insert(LuaItemView item, double? amount = null)
        {
            Mutable();
            if (item == null)
                throw new ScriptRuntimeException("inventory:insert(item, amount?): expected an item instance.");

            GameItem native = item.Mutable();
            int requested = amount.HasValue
                ? LuaValues.Integer(amount.Value, 1, native.unitCount, "inventory:insert amount")
                : native.unitCount;

            return Insert(native, requested);
        }

        public int add(string id, double? amount = null)
        {
            Mutable();
            int? requested = amount.HasValue ? LuaValues.Integer(amount.Value, 1, 1000000, "inventory:add amount") : null;
            GameItem item = _factory.Spawn(id, requested);
            try { return Insert(item, item.unitCount); }
            finally
            {
                // Only objects created by this call may be discarded on failure.
                if (!LuaItemView.IsDestroyed(item) && item.parentInventory == null)
                    LuaItemView.DestroyTree(item);
            }
        }

        internal bool AddContent(string id)
        {
            Mutable();
            GameItem item = _factory.Spawn(id);
            int expected = item.unitCount;
            try { return Insert(item, expected) == expected; }
            finally
            {
                if (!LuaItemView.IsDestroyed(item) && item.parentInventory == null)
                    LuaItemView.DestroyTree(item);
            }
        }

        private int Insert(GameItem item, int amount)
        {
            LuaValues.Range(amount, 1, item.unitCount, "inventory:insert amount");
            if (ReferenceEquals(item.parentInventory, _root) ||
                (item.parentInventory != null && !item.MayRemove()))
                return 0;

            CheckTransfer(item);

            int moved = 0;
            try
            {
                using var fill = FillScope(item);
                for (int attempt = 0; attempt < 128 && moved < amount && !LuaItemView.IsDestroyed(item); attempt++)
                {
                    _factory.ChargeOperation();
                    SlotMarker? slot = _root.TryFindOneValidInventorySlot(item);
                    if (slot == null || slot.numTransfer <= 0)
                        break;

                    if (slot.inventory != null)
                    {
                        LuaInventoryGraph.CheckCycle(slot.inventory, item);
                        LuaInventoryGraph.CheckAccess(slot.inventory, _factory, inserting: true);
                    }
                    int accepted = TransferOnce(slot, item, amount - moved);
                    if (accepted <= 0)
                        break;

                    moved += accepted;
                    if (item.unitCount == 0 && item.parentInventory == null)
                        LuaItemView.DestroyTree(item);
                }
                return moved;
            }
            finally
            {
                if (_owner != null)
                    LuaItemState.RestoreContainerLock(_owner);
            }
        }

        private LuaItemFillScope? FillScope(GameItem item) =>
            _owner != null && _factory.CanFill(_owner) && _root is GameInventory inventory
                ? new LuaItemFillScope(inventory, item)
                : null;

        private static int TransferOnce(SlotMarker slot, GameItem item, int amount)
        {
            GameInventory? previousInventory = item.parentInventory;
            GridShape previousShape = item.modifiedShape.Clone();
            int previousAmount = item.unitCount;
            bool completed = false;
            try
            {
                int moved = slot.TryAcceptOnce(amount);
                completed = moved > 0;
                return moved;
            }
            finally
            {
                if (!completed)
                {
                    GameItem? split = slot.item;
                    if (split != null && !ReferenceEquals(split, item) && split.parentInventory == null && !LuaItemView.IsDestroyed(split))
                    {
                        item.unitCount = previousAmount;
                        LuaItemView.DestroyTree(split);
                        LuaItemState.AmountChanged(item);
                    }
                    if (previousInventory != null && item.parentInventory == null && !LuaItemView.IsDestroyed(item))
                    {
                        item.modifiedShape = previousShape;
                        if (!previousInventory.UncheckedAccept(item))
                            throw new ScriptRuntimeException($"Could not restore '{item.identifier}' after a rejected transfer. The item is still available through its Lua reference; move it to another inventory.");
                    }
                    else if (previousInventory != null && ReferenceEquals(item.parentInventory, previousInventory) && !LuaItemView.IsDestroyed(item))
                    {
                        item.modifiedShape = previousShape;
                        item.Validate();
                    }
                }
            }
        }

        private void CheckTransfer(GameItem item)
        {
            LuaInventoryGraph.CheckCycle(_root, item);
            LuaInventoryGraph.CheckAccess(_root, _factory, inserting: true);
            if (item.parentInventory != null)
                LuaInventoryGraph.CheckAccess(item.parentInventory, _factory, inserting: false);

            // Bound traversal before asking the native recursive slot search.
            LuaInventoryGraph.Items(_root, recursive: true);
        }

        private void Mutable()
        {
            Require();
            if (_readOnly)
                throw new ScriptRuntimeException("Target filters may inspect inventories, but cannot change them.");

            _factory.EnsureMutable();
            _factory.ChargeOperation();
            LuaInventoryGraph.CheckAccess(_root, _factory, inserting: true);
        }

        private void Require()
        {
            if (_owner != null && LuaItemView.IsDestroyed(_owner))
                throw new ScriptRuntimeException($"Container '{_owner.identifier}' has been destroyed.");
                
            foreach (GraphNodeStorage node in LuaInventoryGraph.Ancestors(_root))
                if (node is GameItem item && LuaItemView.IsDestroyed(item))
                    throw new ScriptRuntimeException("This inventory belongs to a destroyed item.");
        }
    }
}
