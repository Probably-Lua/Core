using System;
using System.Runtime.CompilerServices;
using HarmonyLib;

namespace ProbablyLua.Api
{
    [HarmonyPatch(typeof(LockHelper), nameof(LockHelper.UnlockContainer))]
    internal static class LuaItemUnlockedPatch
    {
        [HarmonyPrefix]
        private static void Prefix(GameItem item, bool force, out bool __state) =>
            __state = !force && LuaItemState.IsLocked(item);

        [HarmonyPostfix]
        private static void Postfix(GameItem item, bool __state)
        {
            if (__state && !LuaItemState.IsLocked(item))
                LuaItemLifecycle.Unlocked(item);
        }
    }
}
