using System;
using System.Runtime.CompilerServices;
using HarmonyLib;

namespace ProbablyLua.Api
{
    [HarmonyPatch(typeof(GameItem), nameof(GameItem.GetSplitItem))]
    internal static class LuaItemSplitPatch
    {
        [HarmonyPrefix]
        private static bool Prefix(GameItem __instance, int requestedAmount, ref GameItem __result)
        {
            if (!LuaItemLifecycle.IsManaged(__instance))
                return true;

            __result = LuaItemState.Split(__instance, requestedAmount);
            return false;
        }
    }
}
