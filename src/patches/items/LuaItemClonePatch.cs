using System;
using System.Runtime.CompilerServices;
using HarmonyLib;

namespace ProbablyLua.Api
{
    [HarmonyPatch(typeof(GameItem), "CloneLinked")]
    internal static class LuaItemClonePatch
    {
        [HarmonyPrefix]
        private static void Prefix(out IDisposable __state) => __state = LuaItemLifecycle.Suppress();

        [HarmonyFinalizer]
        private static void Finalizer(IDisposable? __state) => __state?.Dispose();
    }
}
