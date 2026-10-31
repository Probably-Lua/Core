using System;
using HarmonyLib;

namespace ProbablyLua
{
    [HarmonyPatch(typeof(ItemContextHandler), "EqualizeButtonWidths")]
    internal static class ItemContextWidthPatch
    {
        [HarmonyPrefix]
        private static bool Prefix(PixelWindow ___contextWindow)
        {
            if (___contextWindow == null ||
                (!ProbablyLuaSettings.HideUnsupportedContextActions &&
                 !ContextActions.HasAnyActions))
                return true;

            int width = 0;
            foreach (RichTextElement button in ItemContextButtons.Visible(___contextWindow))
            {
                button.SetFixedSize(-1, -1);
                width = Math.Max(width, button.widthPixels);
            }

            foreach (RichTextElement button in ItemContextButtons.Visible(___contextWindow))
                button.SetFixedSize(width, -1);

            return false;
        }
    }
}
