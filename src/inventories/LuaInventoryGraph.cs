using System;
using System.Collections.Generic;
using MoonSharp.Interpreter;

namespace ProbablyLua.Api
{
    internal static class LuaInventoryGraph
    {
        private const int NodeLimit = 4096;

        internal static List<GameItem> Items(GraphNodeStorage root, bool recursive)
        {
            var result = new List<GameItem>();
            var pending = new Queue<GraphNodeStorage>();
            var seen = new HashSet<GraphNodeStorage>();
            pending.Enqueue(root);
            while (pending.Count != 0)
            {
                GraphNodeStorage node = pending.Dequeue();
                if (!seen.Add(node))
                    continue;

                CheckSize(seen.Count);
                if (node is GameItem item)
                {
                    if (LuaItemView.IsDestroyed(item))
                        continue;

                    result.Add(item);
                    // Item operations traverse content inventories, not internal
                    // examination/assembly windows belonging to the game.
                    if (recursive && item.contentWindow != null)
                        pending.Enqueue(item.contentWindow);

                    continue;
                }
                foreach (GraphNodeStorage child in node.children)
                    pending.Enqueue(child);
            }
            return result;
        }

        internal static List<GraphNodeStorage> Ancestors(GraphNodeStorage root)
        {
            var result = new List<GraphNodeStorage>();
            var pending = new Queue<GraphNodeStorage>();
            var seen = new HashSet<GraphNodeStorage>();
            pending.Enqueue(root);
            while (pending.Count != 0)
            {
                GraphNodeStorage node = pending.Dequeue();
                if (!seen.Add(node))
                    continue;

                CheckSize(seen.Count);
                result.Add(node);
                foreach (GraphNodeStorage parent in node.parents)
                    pending.Enqueue(parent);
            }
            return result;
        }

        internal static void CheckAccess(GraphNodeStorage node, LuaItemFactory factory, bool inserting)
        {
            foreach (GraphNodeStorage ancestor in Ancestors(node))
            {
                if (ancestor is GameItem item && LuaItemState.IsLocked(item) &&
                    !(inserting && factory.CanFill(item)))
                    throw new ScriptRuntimeException($"Container '{item.identifier}' is locked. Unlock it before changing its contents.");

                if (ancestor is GameInventory inventory &&
                    (inserting ? inventory.IsInsertLocked() : inventory.IsRemoveLocked()))
                    throw new ScriptRuntimeException(inserting ? "This inventory is locked for insertion." : "This inventory is locked for removal.");
            }
        }

        internal static void CheckCycle(GraphNodeStorage destination, GameItem source)
        {
            foreach (GraphNodeStorage ancestor in Ancestors(destination))
                if (ReferenceEquals(ancestor, source))
                    throw new ScriptRuntimeException("Cannot put a container inside itself or one of its descendants.");
        }

        private static void CheckSize(int size)
        {
            if (size > NodeLimit)
                throw new ScriptRuntimeException($"Inventory operation exceeded {NodeLimit} graph nodes. Use a smaller inventory.");
        }
    }
}
