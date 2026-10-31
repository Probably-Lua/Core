using HarmonyLib;
using ProbablyLua.Api;

namespace ProbablyLua
{
    [HarmonyPatch(typeof(ItemSelectHandler), "RefreshHighlights")]
    internal static class LuaSelectionHighlightsPatch
    {
        [HarmonyPrefix]
        private static void Prefix(GameItem sourceItem, out LuaTargetProbeScope? __state) =>
            __state = LuaTargetProbeScope.Begin(sourceItem);

        [HarmonyFinalizer]
        private static void Finalizer(LuaTargetProbeScope? __state) => __state?.Dispose();
    }
}
