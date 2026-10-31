using HarmonyLib;

namespace ProbablyLua
{
    // Native lock handling asks SetSpriteAndShape for the opened sprite.
    // Mod textures are not Resources atlases and need the supported mod path.
    [HarmonyPatch(typeof(GameItem), nameof(GameItem.SetSpriteAndShape))]
    internal static class LuaItemSpritePatch
    {
        [HarmonyPrefix]
        private static bool Prefix(GameItem __instance, string spriteAtlasPath, string spritePath, ref GameItem __result)
        {
            if (!__instance.useSpriteFromMod)
                return true;

            LoadedMod? mod = ModLoader.Mods.Find(m => m.Manifest.ID == spriteAtlasPath);
            if (mod == null || !mod.Sprites.ContainsKey(spritePath))
                return true;

            __result = __instance.SetSpriteAndShapeFromMod(spriteAtlasPath, spritePath);
            return false;
        }
    }
}
