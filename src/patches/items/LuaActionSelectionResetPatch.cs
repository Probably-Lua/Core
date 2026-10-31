using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using MoonSharp.Interpreter;

namespace ProbablyLua.Api
{
    [HarmonyPatch(typeof(ItemSelectHandler), "OnEventReset")]
    internal static class LuaActionSelectionResetPatch
    {
        [HarmonyPostfix]
        private static void Postfix() => LuaItemActions.SelectionReset();
    }
}
