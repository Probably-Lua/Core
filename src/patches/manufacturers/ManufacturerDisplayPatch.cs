using System;
using System.Collections.Generic;
using HarmonyLib;

namespace ProbablyLua.Api
{
    [HarmonyPatch(typeof(GameItem), nameof(GameItem.DisplayManufacturer))]
    internal static class ManufacturerDisplayPatch
    {
        internal static ManufacturerRegistry? Registry { get; set; }

        private static bool Prefix(GameItem __instance, RichTextBuilder builder, string manufacturer)
        {
            // When ProbablyLua is disabled, leave the game's renderer untouched.
            if (Registry == null)
                return true;

            Registry.Render(__instance, builder, manufacturer);
            return false;
        }
    }
}
