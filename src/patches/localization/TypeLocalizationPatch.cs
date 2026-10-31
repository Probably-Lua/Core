using System;
using HarmonyLib;

namespace ProbablyLua.Api
{
    // Called by vanilla tooltips for *all* type strings, including custom ones.
    [HarmonyPatch(typeof(TypeHelper), nameof(TypeHelper.GetTypeDisplayName))]
    internal static class TypeLocalizationPatch
    {
        internal static LangRegistry? Registry;

        [HarmonyPrefix]
        private static bool Prefix(string typeConst, ref string __result)
        {
            if (Registry == null || string.IsNullOrWhiteSpace(typeConst))
                return true;

            string type = typeConst.Trim();
            __result = Registry.Resolve("base:type." + type + ".name", type,
                "Mechanic", "type_" + type.ToLowerInvariant());
            return false;
        }
    }
}
