using HarmonyLib;
using ProbablyLua.Api;

namespace ProbablyLua
{
    [HarmonyPatch(typeof(ItemMouseDragHandler), "StartDrag")]
    internal static class LuaDragStartPatch
    {
        // StartDrag scans targets before it sets IsDraggingItem. Without this
        // scope, Lua-only targets are missing from the game's cleanup list.
        [HarmonyPrefix]
        private static void Prefix(GameItem ___currentItem, out LuaTargetProbeScope? __state) =>
            __state = LuaTargetProbeScope.Begin(___currentItem, startingDrag: true);

        [HarmonyFinalizer]
        private static void Finalizer(LuaTargetProbeScope? __state) => __state?.Dispose();
    }
}
