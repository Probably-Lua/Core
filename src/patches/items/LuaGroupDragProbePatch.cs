using System.Collections.Generic;
using HarmonyLib;
using ProbablyLua.Api;

namespace ProbablyLua
{
    [HarmonyPatch(typeof(ItemMultiSelectHandler), "UpdateDropTarget")]
    internal static class LuaGroupDragProbePatch
    {
        [HarmonyPrefix]
        private static void Prefix(List<GameItemElement> ___selectedItems, out LuaTargetProbeScope? __state) =>
            __state = LuaTargetProbeScope.BeginGroup(___selectedItems);

        [HarmonyFinalizer]
        private static void Finalizer(LuaTargetProbeScope? __state) => __state?.Dispose();
    }
}
