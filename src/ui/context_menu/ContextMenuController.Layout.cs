using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using ProbablyLua.Api;
using UnityEngine;

namespace ProbablyLua
{
    // Owns dynamic menu grids and restores the game's original layout.
    internal static partial class ContextMenuController
    {
        private static RichTextElement GetOrCreateButton(State state, string actionId)
        {
            if (state.ExtraButtons.TryGetValue(actionId, out RichTextElement button))
                return button;

            button = new RichTextElement();
            button.handler.gameObject.name = "nodeButton_" + actionId;
            foreach (Transform node in button.handler.GetComponentsInChildren<Transform>(true))
                node.gameObject.tag = "ContextMenuNode";

            state.ExtraButtons.Add(actionId, button);
            return button;
        }

        private static void ShowRows(State state, List<Row> rows)
        {
            GridPixelElement grid = state.DynamicGrid != null &&
                state.DynamicGrid.gridHeight == Math.Max(1, rows.Count)
                ? state.DynamicGrid
                : new GridPixelElement(1, Math.Max(1, rows.Count));

            GridPixelElement previous = state.Window.childElement as GridPixelElement
                ?? state.OriginalGrid;

            DetachAll(previous);
            if (!ReferenceEquals(grid, previous))
                DetachAll(grid);

            if (!ReferenceEquals(state.Window.childElement, grid))
                state.Window.Attach(grid);

            foreach (Row row in rows)
            {
                row.Button.handler.gameObject.SetActive(true);
                grid.Attach(row.Button);
            }

            foreach (Row row in state.Vanilla)
                if (!rows.Contains(row))
                    row.Button.handler.gameObject.SetActive(false);

            foreach (var pair in state.ExtraButtons)
                if (!state.ActiveOverrides.ContainsKey(pair.Value))
                    pair.Value.handler.gameObject.SetActive(false);

            grid.Validate();

            if (state.DynamicGrid != null && !ReferenceEquals(state.DynamicGrid, grid))
                UnityEngine.Object.Destroy(state.DynamicGrid.handler.gameObject);

            state.DynamicGrid = grid;
        }

        private static void DetachAll(GridPixelElement grid)
        {
            for (int row = 0; row < grid.gridHeight; row++)
                for (int col = 0; col < grid.gridWidth; col++)
                    if (grid.GetElement(col, row) is PixelElement element)
                        grid.Detach(element);
        }

        private static void RestoreGrid(State state)
        {
            GridPixelElement current = state.Window.childElement as GridPixelElement
                ?? state.OriginalGrid;

            if (!ReferenceEquals(current, state.OriginalGrid))
            {
                DetachAll(current);
                state.Window.Attach(state.OriginalGrid);
            }

            DetachAll(state.OriginalGrid);
            foreach (Row row in state.Vanilla)
            {
                row.Button.handler.gameObject.SetActive(true);
                state.OriginalGrid.Attach(row.Button);
            }

            foreach (var entry in state.ExtraButtons)
                entry.Value.handler.gameObject.SetActive(false);

            if (state.DynamicGrid != null)
            {
                UnityEngine.Object.Destroy(state.DynamicGrid.handler.gameObject);
                state.DynamicGrid = null;
            }

            state.ActiveOverrides.Clear();
        }

        internal static void RestoreCurrent()
        {
            ItemContextHandler? handler = ItemContextHandler.current;
            if (handler != null && States.TryGetValue(handler, out State state))
                RestoreGrid(state);
        }
    }
}
