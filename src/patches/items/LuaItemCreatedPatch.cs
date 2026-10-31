using System;
using System.Runtime.CompilerServices;
using HarmonyLib;

namespace ProbablyLua.Api
{
    [HarmonyPatch(typeof(DirectoryMaster), nameof(DirectoryMaster.Item), new[] { typeof(string), typeof(bool) })]
    internal static class LuaItemCreatedPatch
    {
        [HarmonyPostfix]
        private static void Postfix(GameItem __result) => LuaItemLifecycle.Created(__result);
    }
}
