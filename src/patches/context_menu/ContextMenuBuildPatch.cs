using System;
using HarmonyLib;

namespace ProbablyLua
{
    // The game's own menu logic still owns its buttons and their callbacks.
    // The registry decides which rows belong in the menu; it never reimplements
    // vanilla item interactions.
    [HarmonyPatch(typeof(ItemContextHandler), "UpdateConditions")]
    internal static class ContextMenuBuildPatch
    {
        [HarmonyPostfix]
        private static void Postfix(ItemContextHandler __instance) => ContextMenuController.Rebuild(__instance);
    }
}
