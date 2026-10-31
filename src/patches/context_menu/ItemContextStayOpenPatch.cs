using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using TMPro;
using UnityEngine;

namespace ProbablyLua
{
    // Keep the item context menu open while the pointer moves between rows
    // or leaves its bounds. The vanilla update still refreshes hover colors.
    [HarmonyPatch(typeof(ItemContextHandler), "OnEventUpdate")]
    internal static class ItemContextStayOpenPatch
    {
        [HarmonyPrefix]
        private static void Prefix(GameItem ___currentItem, PixelWindow ___contextWindow, ref bool ___delayFrame)
        {
            if (___currentItem != null && ___contextWindow != null && ___contextWindow.IsVisible())
            {
                // Vanilla only closes the menu when delayFrame is false.
                ___delayFrame = true;
            }
        }
    }
}
