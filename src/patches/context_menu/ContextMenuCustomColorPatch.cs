using System;
using HarmonyLib;

namespace ProbablyLua
{
    [HarmonyPatch(typeof(ItemContextHandler), "UpdateSelectButtonColor")]
    internal static class ContextMenuCustomColorPatch
    {
        // Run before the existing polish postfix, which recognizes the vanilla
        // enabled/disabled colors and draws the appropriate hover background.
        [HarmonyPriority(Priority.First)]
        [HarmonyPostfix]
        private static void Postfix(ItemContextHandler __instance, RichTextElement selectedButton)
            => ContextMenuController.UpdateCustomColors(__instance, selectedButton);
    }
}
