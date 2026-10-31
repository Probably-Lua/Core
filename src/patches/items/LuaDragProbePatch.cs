using HarmonyLib;
using ProbablyLua.Api;

namespace ProbablyLua
{
    [HarmonyPatch(typeof(ItemMouseDragHandler), "Drag")]
    internal static class LuaDragProbePatch
    {
        [HarmonyPrefix]
        private static void Prefix(GameItem ___currentItem, out LuaTargetProbeScope? __state) =>
            __state = LuaTargetProbeScope.Begin(___currentItem);

        [HarmonyFinalizer]
        private static void Finalizer(LuaTargetProbeScope? __state) => __state?.Dispose();
    }
}
