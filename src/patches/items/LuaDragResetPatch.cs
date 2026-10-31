using System.Collections.Generic;
using HarmonyLib;

namespace ProbablyLua
{
    [HarmonyPatch(typeof(ItemMouseDragHandler), "OnEventReset")]
    internal static class LuaDragResetPatch
    {
        // AbortDrag also reaches this reset after a source item is destroyed.
        [HarmonyFinalizer]
        private static void Finalizer(List<TreeNodeRender> ___highlightedMayNodes) =>
            LuaDragHighlights.Clear(___highlightedMayNodes);
    }
}
