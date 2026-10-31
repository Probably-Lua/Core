using System;
using System.Collections.Generic;
using HarmonyLib;

namespace ProbablyLua.Api
{
    // Only a synchronous container initializer may insert newly constructed
    // owned children into a not-yet-owned crate. Do not change either object's
    // ownership tags: buying a crate relies on its children keeping their tags.
    internal sealed class LuaItemFillScope : IDisposable
    {
        [ThreadStatic] private static List<LuaItemFillScope>? _active;
        private readonly GameInventory _inventory;
        private readonly GameItem _item;
        private readonly Func<GameItem, GameInventory, bool>? _previousRule;
        private bool _disposed;

        internal LuaItemFillScope(GameInventory inventory, GameItem item)
        {
            _inventory = inventory;
            _item = item;
            _previousRule = inventory.mayInventoryAddItemFunc;
            inventory.mayInventoryAddItemFunc = (_, _) => true;
            (_active ??= new List<LuaItemFillScope>()).Add(this);
        }

        internal static bool Allows(GameItem item, GameInventory inventory)
        {
            if (_active == null)
                return false;

            foreach (LuaItemFillScope scope in _active)
                if (ReferenceEquals(scope._item, item) && ReferenceEquals(scope._inventory, inventory))
                    return true;

            return false;
        }

        public void Dispose()
        {
            if (_disposed)
                return;
                
            _disposed = true;
            _active!.Remove(this);
            _inventory.mayInventoryAddItemFunc = _previousRule;
        }
    }
}
