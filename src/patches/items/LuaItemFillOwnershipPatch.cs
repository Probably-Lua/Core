using System;
using System.Collections.Generic;
using HarmonyLib;

namespace ProbablyLua.Api
{
    [HarmonyPatch(typeof(GeneralHelper), nameof(GeneralHelper.MayOwnershipAcceptItem))]
    internal static class LuaItemFillOwnershipPatch
    {
        [HarmonyPrefix]
        private static bool Prefix(GameItem item, GameInventory inventory, ref bool __result)
        {
            if (!LuaItemFillScope.Allows(item, inventory))
                return true;

            __result = true;
            return false;
        }
    }
}
