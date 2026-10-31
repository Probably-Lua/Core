using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using ProbablyLua.Api;
using UnityEngine;

namespace ProbablyLua
{
    // The vanilla adapter is the only part that knows the game's seven fields.
    // Both vanilla and mod-added rows are keyed by their string action IDs.
    internal static partial class ContextMenuController
    {
        private sealed class VanillaAction
        {
            internal readonly string Id;
            internal readonly FieldInfo? Field;
            internal readonly Func<GameItem, bool> HasHandler;

            internal VanillaAction(string id, FieldInfo? field, Func<GameItem, bool> hasHandler)
            {
                Id = id;
                Field = field;
                HasHandler = hasHandler;
            }
        }
        private static readonly FieldInfo? CurrentItemField =
            AccessTools.Field(typeof(ItemContextHandler), "currentItem");
        private static readonly FieldInfo? ContextWindowField =
            AccessTools.Field(typeof(ItemContextHandler), "contextWindow");

        private static readonly VanillaAction[] Vanilla =
        {
            new("base:open", AccessTools.Field(typeof(ItemContextHandler), "buttonOpen"), i => i.contentWindow != null),
            new("base:use", AccessTools.Field(typeof(ItemContextHandler), "buttonUse"), HasSelect),
            new("base:equip", AccessTools.Field(typeof(ItemContextHandler), "buttonEquip"), HasSelect),
            new("base:activate", AccessTools.Field(typeof(ItemContextHandler), "buttonActivate"), HasActivate),
            new("base:toggle", AccessTools.Field(typeof(ItemContextHandler), "buttonToggle"), HasToggle),
            new("base:unload", AccessTools.Field(typeof(ItemContextHandler), "buttonUnload"), i => i.contentWindow != null),
            new("base:pin", AccessTools.Field(typeof(ItemContextHandler), "buttonPin"), _ => true)
        };

        private static bool Has(Delegate? a, Delegate? b) => a != null || b != null;

        // Presence of a handler, never its current boolean return value.
        private static bool HasSelect(GameItem item) =>
            Has(item.maySelectSlotItemFunc, item.parentInventory?.maySelectSlotItemFunc) ||
            Has(item.canSelectSlotItemFunc, item.parentInventory?.canSelectSlotItemFunc);

        private static bool HasActivate(GameItem item) =>
            Has(item.onActivateSlotItemFunc, item.parentInventory?.onActivateSlotItemFunc);

        private static bool HasToggle(GameItem item) =>
            Has(item.onToggleSlotItemFunc, item.parentInventory?.onToggleSlotItemFunc);

        private static GameItem? CurrentItem(ItemContextHandler handler) =>
            CurrentItemField?.GetValue(handler) as GameItem;
    }
}
