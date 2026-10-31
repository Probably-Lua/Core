using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using TMPro;
using UnityEngine;

namespace ProbablyLua
{
    // A click outside dismisses the menu, but it does not eat the click:
    // right-clicking another item can still open that item's menu immediately.
    [HarmonyPatch(typeof(ItemContextHandler), "OnEventPress")]
    internal static class ItemContextClickAwayPatch
    {
        [HarmonyPrefix]
        private static void Prefix(ItemContextHandler __instance, ISet<KeyCode> keysPressed, PixelWindow ___contextWindow)
        {
            if (___contextWindow == null || !___contextWindow.IsVisible())
                return;

            if (!keysPressed.Contains(__instance.key) &&
                !keysPressed.Contains(__instance.triggerKey))
                return;

            if (__instance.FindContextMenuNode(
                    Input.mousePosition, __instance.overlayCanvas) == null)
            {
                __instance.Stop();
            }
        }
    }
}
