using System;
using System.Collections.Generic;
using MoonSharp.Interpreter;

namespace ProbablyLua.Api
{
    internal sealed partial class LuaItemFactory
    {
        internal void Initialize(LuaItemDefinition definition, GameItem item)
        {
            EnsureMutable();
            try
            {
                _filling.Add(item);
                if (definition.on_spawn != null)
                    _callbacks.Invoke(definition.on_spawn, View(item));

                if (LuaItemView.IsDestroyed(item))
                    throw new ScriptRuntimeException("on_spawn destroyed the item being created.");
            }
            catch (Exception exception)
            {
                LuaItemView.DestroyTree(item);
                throw new ScriptRuntimeException($"Item '{definition.id}'.on_spawn: {LuaErrorFormatter.Describe(exception)}");
            }
            finally
            {
                _filling.Remove(item);
                LuaItemState.RestoreContainerLock(item);
            }
        }

        private void AttachLifecycle(LuaItemDefinition definition, GameItem item)
        {
            if (definition.on_loaded != null)
                item.onLoaded += current => Notify("on_loaded", definition, current, definition.on_loaded);

            if (definition.on_second != null)
                item.onOneSecondItemFunc += (current, _, _) => Notify("on_second", definition, current, definition.on_second);

            if (definition.on_turn != null)
                item.onTurnEndSlotItemFunc += (current, _, _) => Notify("on_turn", definition, current, definition.on_turn);

            if (definition.on_cycle != null)
                item.onCycleEndSlotItemFunc += (current, _, _) => Notify("on_cycle", definition, current, definition.on_cycle);

            if (definition.on_added != null)
                item.onSlotAddItemFunc += (current, inventory, _) =>
                    QueueNotification("on_added", definition, current, definition.on_added, Inventory(inventory));

            if (definition.on_removed != null)
                item.onSlotRemoveItemFunc += (current, inventory, _) =>
                    QueueNotification("on_removed", definition, current, definition.on_removed, Inventory(inventory));

            if (definition.on_amount_changed != null)
                item.onChangeAmountItemFunc += current =>
                    QueueNotification("on_amount_changed", definition, current, definition.on_amount_changed);
        }

        private void Notify(string eventName, LuaItemDefinition definition, GameItem item, DynValue? callback)
        {
            string key = definition.id + "/" + eventName;
            if (!_active || callback == null || _callbackErrors.Contains(key) || LuaItemView.IsDestroyed(item))
                return;

            bool filling = eventName == "on_loaded" && _filling.Add(item);
            try
            {
                if (!_callbacks.TryInvoke($"Item '{definition.id}'.{eventName}", callback, View(item)))
                    _callbackErrors.Add(key);
            }
            finally
            {
                if (filling)
                {
                    _filling.Remove(item);
                    LuaItemState.RestoreContainerLock(item);
                }
            }
        }

        internal void QueueNotification(string eventName, LuaItemDefinition definition, GameItem item,
            DynValue? callback, LuaInventoryView? inventory = null)
        {
            if (!_active || SaveManager.IsDecoding || callback == null)
                return;

            if (_pendingNotifications >= 256)
            {
                if (_callbackErrors.Add("notification_queue"))
                    _warning("Item event queue exceeded 256 pending notifications; excess events were dropped. Check recursive item changes.");

                return;
            }

            _pendingNotifications++;
            // Native inventory handlers fire while the graph is being changed.
            // Lua receives the event next update, after the operation is complete.
            LetterboxManager.RunNextUpdate(() =>
            {
                _pendingNotifications--;
                string key = definition.id + "/" + eventName;
                if (!_active || LuaItemView.IsDestroyed(item) || _callbackErrors.Contains(key))
                    return;

                object[] args = inventory == null ? new object[] { View(item) } : new object[] { View(item), inventory };
                if (!_callbacks.TryInvoke($"Item '{definition.id}'.{eventName}", callback, args))
                    _callbackErrors.Add(key);
            });
        }
    }
}
