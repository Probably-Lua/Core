using System;
using HarmonyLib;

namespace ProbablyLua
{
    [HarmonyPatch(typeof(ItemContextHandler), "TriggerButton")]
    internal static class ContextMenuCustomClickPatch
    {
        [HarmonyPrefix]
        private static bool Prefix(ItemContextHandler __instance, RichTextElement button) => !ContextMenuController.TryClick(__instance, button);
    }
}
