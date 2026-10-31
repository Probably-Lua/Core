using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using TMPro;
using UnityEngine;

namespace ProbablyLua
{
    // Add breathing room. The shared width-equalizer patch still determines which
    // buttons are visible; this postfix only pads its resulting row width.
    [HarmonyPatch(typeof(ItemContextHandler), "EqualizeButtonWidths")]
    internal static class ItemContextTextPaddingPatch
    {
        [HarmonyPostfix]
        private static void Postfix(PixelWindow ___contextWindow)
        {
            int width = 0;
            foreach (RichTextElement button in ItemContextButtons.Visible(___contextWindow))
                width = Mathf.Max(width, button.widthPixels);

            if (width == 0)
                return;

            const int horizontalPadding = 14;
            foreach (RichTextElement button in ItemContextButtons.Visible(___contextWindow))
                button.SetFixedSize(width + horizontalPadding, -1);
        }
    }
}
