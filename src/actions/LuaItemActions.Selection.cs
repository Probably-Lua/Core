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
        private sealed class Selection
        {
            internal readonly LuaItemActions Owner;
            internal readonly GameItem Source;
            internal readonly string Id;
            internal readonly LuaActionKind Kind;

            internal Selection(LuaItemActions owner, GameItem source, string id,
                LuaActionKind kind)
            {
                Owner = owner;
                Source = source;
                Id = id;
                Kind = kind;
            }
        }

        // The game has one ItemSelectHandler and only one selected source.
        // _pending is necessary: RefreshHighlights() runs *inside* TrySelectItem().
        private static Selection? _active;
        private static Selection? _pending;
        private static readonly FieldInfo? SelectedItemField = AccessTools.Field(typeof(ItemSelectHandler), "currentItem");

        // Resetting selection is a native operation, including during
        // TryEquipItem(). Do not erase a pending action during that internal reset.
        internal static void SelectionReset() => _active = null;

        private static bool IsCurrentItem(GameItem source)
        {
            ItemSelectHandler? selector = ItemSelectHandler.current;
            return selector != null && SelectedItemField != null &&
                ReferenceEquals(SelectedItemField.GetValue(selector), source);
        }

        private string? SelectedAction(GameItem source)
        {
            Selection? selection = _pending != null &&
                ReferenceEquals(_pending.Source, source) ? _pending : _active;
                
            if (selection == null || !ReferenceEquals(selection.Owner, this) ||
                !ReferenceEquals(selection.Source, source) || !IsCurrentItem(source))
                return null;

            if (ReferenceEquals(selection, _active) &&
                ItemSelectHandler.current!.IsEquipped != (selection.Kind == LuaActionKind.Equip))

                return null;

            return selection.Id;
        }

        private void StartSelection(GameItem source, string id, LuaActionKind kind)
        {
            ItemSelectHandler? selector = ItemSelectHandler.current;
            if (selector == null || SelectedItemField == null)
                return;

            Selection? previous = _active;
            var selection = new Selection(this, source, id, kind);
            _pending = selection;
            try
            {
                if (kind == LuaActionKind.Equip)
                    selector.TryEquipItem(source);

                else
                    selector.TrySelectItem(source);

                if (IsCurrentItem(source) &&
                    selector.IsEquipped == (kind == LuaActionKind.Equip))
                    _active = selection;

                else if (previous != null && IsCurrentItem(previous.Source))
                    _active = previous;

                else
                    _active = null;
            }
            finally
            {
                _pending = null;
            }
        }

        private static bool IsDragged(GameItem source)
        {
            if (LuaTargetProbeScope.IsStartingDrag(source))
                return true;

            ItemMouseDragHandler? drag = ItemMouseDragHandler.current;
            if (drag != null && drag.IsDraggingItem &&
                ReferenceEquals(drag.GetCurrentItem(), source))
                return true;

            ItemMultiSelectHandler? group = ItemMultiSelectHandler.current;
            return group != null && group.IsGroupDraggingItem(source);
        }

        private LuaActionBinding? TargetHandler(LuaItemDefinition definition, GameItem source, out string id)
        {
            // Native dragging and menu selection share GameItem.Target(). Route by
            // the actual input owner, never by whichever action was added first.
            if (IsDragged(source))
            {
                id = "on_drop";
                return definition.DropHandler;
            }
            string? selected = SelectedAction(source);
            id = selected ?? "";
            return selected == null ? null : Handler(definition, selected);
        }
    }
}
