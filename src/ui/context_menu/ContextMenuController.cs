using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using ProbablyLua.Api;
using UnityEngine;

namespace ProbablyLua
{
    // Builds action rows and dispatches menu clicks by string action ID.
    internal static partial class ContextMenuController
    {
        private sealed class Row
        {
            internal readonly string Id;
            internal readonly RichTextElement Button;
            internal Row(string id, RichTextElement button)
            {
                Id = id;
                Button = button;
            }
        }

        private sealed class State
        {
            internal PixelWindow Window = null!;
            internal GridPixelElement OriginalGrid = null!;
            internal GridPixelElement? DynamicGrid;
            internal readonly List<Row> Vanilla = new();
            internal readonly Dictionary<string, RichTextElement> ExtraButtons =
                new(StringComparer.OrdinalIgnoreCase);
            internal readonly Dictionary<RichTextElement, ContextAction> ActiveOverrides = new();
        }

        private static readonly ConditionalWeakTable<ItemContextHandler, State> States = new();

        private static State? Get(ItemContextHandler handler)
        {
            PixelWindow? window = ContextWindowField?.GetValue(handler) as PixelWindow;
            if (window == null)
                return null;

            State state = States.GetValue(handler, _ => new State());
            if (ReferenceEquals(window, state.Window))
                return state;

            state.Window = window;
            state.DynamicGrid = null;
            state.Vanilla.Clear();
            state.ExtraButtons.Clear();
            state.ActiveOverrides.Clear();
            if (window.childElement is not GridPixelElement grid)
                return null;

            state.OriginalGrid = grid;
            foreach (VanillaAction action in Vanilla)
            {
                if (action.Field?.GetValue(handler) is not RichTextElement button)
                    return null;

                state.Vanilla.Add(new Row(action.Id, button));
            }

            return state;
        }

        internal static void Rebuild(ItemContextHandler handler)
        {
            GameItem? item = CurrentItem(handler);
            if (item == null)
                return;

            State? state = Get(handler);
            if (state == null || state.Vanilla.Count != Vanilla.Length)
                return;

            string itemId = item.identifier ?? "";
            string qualifiedId = itemId.Contains(":") ? itemId : "base:" + itemId;
            List<ContextAction> actions = ContextActions.SnapshotFor(qualifiedId);

            List<Row> rows = BuildRows(state, item, actions);
            ShowRows(state, rows);
        }

        private static List<Row> BuildRows(
            State state, GameItem item, List<ContextAction> actions)
        {
            var overrides = new Dictionary<string, ContextAction>(StringComparer.OrdinalIgnoreCase);
            foreach (ContextAction action in actions)
                overrides[action.Id] = action;

            var rows = new List<Row>();
            state.ActiveOverrides.Clear();

            for (int i = 0; i < Vanilla.Length; i++)
            {
                VanillaAction original = Vanilla[i];
                Row row = state.Vanilla[i];

                if (overrides.TryGetValue(original.Id, out ContextAction replacement))
                {
                    if (replacement.IsSuppressed)
                        continue;

                    rows.Add(row);
                    state.ActiveOverrides[row.Button] = replacement;
                    continue;
                }

                if (!ProbablyLuaSettings.HideUnsupportedContextActions ||
                    original.HasHandler(item))
                    rows.Add(row);
            }

            foreach (ContextAction action in actions)
            {
                // Custom actions get their own row; built-in actions reuse
                // the existing game button instead of creating duplicates.
                if (ResourceIds.IsVanillaAction(action.Id) || action.IsSuppressed)
                    continue;

                RichTextElement button = GetOrCreateButton(state, action.Id);
                rows.Add(new Row(action.Id, button));
                state.ActiveOverrides[button] = action;
            }

            rows.Sort((a, b) => StringComparer.OrdinalIgnoreCase.Compare(a.Id, b.Id));
            return rows;
        }

        internal static void UpdateCustomColors(ItemContextHandler handler, RichTextElement selected)
        {
            if (!States.TryGetValue(handler, out State state) ||
                state.ActiveOverrides.Count == 0)
                return;

            GameItem? item = CurrentItem(handler);
            if (item == null)
                return;

            foreach (var pair in state.ActiveOverrides)
            {
                RichTextElement button = pair.Key;
                ContextAction action = pair.Value;
                try
                {
                    // When an item replaces a vanilla action, its own label and
                    // handler replace the vanilla state-sensitive presentation too.
                    // The polish postfix handles hover styling for these rows.
                    bool enabled = action.CanExecute?.Invoke(item) ?? true;
                    button.SetText(action.Label?.Invoke(item) ?? action.Id, 10000,
                        enabled ? RenderHandler.ColorPalette.LightGray : RenderHandler.ColorPalette.DarkGray);
                }
                catch (Exception exception)
                {
                    Debug.LogError($"ProbablyLua context action '{action.Id}' render failed: {exception}");
                    button.SetText(action.Id, 10000, RenderHandler.ColorPalette.DarkGray);
                }
            }
        }

        // True means a registered override handled the click. Vanilla falls
        // through to the game's own TriggerButton implementation unchanged.
        internal static bool TryClick(ItemContextHandler handler, RichTextElement button)
        {
            if (!States.TryGetValue(handler, out State state) ||
                !state.ActiveOverrides.TryGetValue(button, out ContextAction action))
                return false;

            GameItem? item = CurrentItem(handler);
            if (item == null || !button.handler.gameObject.activeSelf ||
                !ContextActions.IsRegistered(action))
                return true;

            try
            {
                if (action.CanExecute != null && !action.CanExecute(item))
                    return true;

                handler.Stop();
                action.Execute?.Invoke(item);
            }
            catch (Exception exception)
            {
                Debug.LogError($"ProbablyLua context action '{action.Id}' failed: {exception}");
            }

            return true;
        }
    }
}
