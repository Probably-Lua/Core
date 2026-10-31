using System;
using System.Runtime.CompilerServices;
using HarmonyLib;

namespace ProbablyLua.Api
{
    [HarmonyPatch(typeof(GeneralHelper), nameof(GeneralHelper.GetDisplayNameFromID))]
    internal static class LuaItemNameProbePatch
    {
        [HarmonyPrefix]
        private static void Prefix(out IDisposable __state) => __state = LuaItemLifecycle.Suppress();

        [HarmonyFinalizer]
        private static void Finalizer(IDisposable? __state) => __state?.Dispose();
    }
}
