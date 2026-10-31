using System.Collections.Generic;
using HarmonyLib;

namespace ProbablyLua
{
    [HarmonyPatch(typeof(ItemMouseDragHandler), "EndDrag")]
    internal static class LuaDragEndPatch
    {
        [HarmonyFinalizer]
        private static void Finalizer(List<TreeNodeRender> ___highlightedMayNodes) =>
            LuaDragHighlights.Clear(___highlightedMayNodes);
    }
}
