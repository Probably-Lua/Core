using System.Collections.Generic;
using System.Runtime.CompilerServices;
using HarmonyLib;

namespace ProbablyLua
{
    // The current game has a customer-generation hook, but no trade-intro hook.
    // Attach after blueprint construction; all stock and probability rules stay in Lua.
    [HarmonyPatch(typeof(StoreClient), nameof(StoreClient.CompleteClientCreation))]
    internal static class LuaCustomerTradePatch
    {
        internal static LuaHost? Host { get; set; }
        private static readonly ConditionalWeakTable<StoreClient, object> Hooked = new();

        [HarmonyPostfix]
        private static void Postfix(StoreClient __instance)
        {
            if (Host == null || __instance.mainDialogue == null || Hooked.TryGetValue(__instance, out _))
                return;

            Dialogue last = __instance.mainDialogue;
            var visited = new HashSet<Dialogue> { last };
            while (last.nextDialogue != null)
            {
                if (!visited.Add(last.nextDialogue))
                    return;

                last = last.nextDialogue;
            }

            Hooked.Add(__instance, new object());
            string id = __instance.identifier.Contains(":") ? __instance.identifier : "base:" + __instance.identifier;
            last.endAction += () => Host?.Emit("customer_trade", id);
        }
    }
}
