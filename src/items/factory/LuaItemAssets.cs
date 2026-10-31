using System;
using MoonSharp.Interpreter;

namespace ProbablyLua.Api
{
    internal sealed class LuaItemAssets
    {
        private readonly string _modId;

        internal LuaItemAssets(string modId) => _modId = modId;

        internal void Validate(LuaItemDefinition definition)
        {
            if (!string.IsNullOrEmpty(definition.sprite))
                RequireSprite(definition.sprite);

            if (!string.IsNullOrEmpty(definition.container.opened_sprite))
                RequireSprite(definition.container.opened_sprite);

            ApplySounds(definition.sounds, (_, _) => { });
        }

        private ResourceId RequireSprite(string key)
        {
            ResourceId id = ResourceIds.Parse(key, _modId);
            LoadedMod? mod = ModLoader.Mods.Find(m => m.Manifest.ID == id.ModId);
            if (mod == null || !mod.Sprites.ContainsKey(id.LocalId))
                throw new ScriptRuntimeException($"Sprite '{id.FullId}' was not found. Put '{id.LocalId}.png' in that mod's Textures/Items/ folder; omit the extension in Lua.");

            return id;
        }

        internal void SetSprite(GameItem item, string key)
        {
            ResourceId id = RequireSprite(key);
            item.SetSpriteAndShapeFromMod(id.ModId, id.LocalId);
        }

        internal string? Sound(string? key)
        {
            if (key == null || key.Length == 0)
                return null;

            ResourceId id = ResourceIds.Parse(key, _modId);
            if (id.IsBase)
                return id.LocalId;

            LoadedMod? mod = ModLoader.Mods.Find(m => m.Manifest.ID == id.ModId);
            if (mod == null || !mod.Sounds.TryGetValue(id.LocalId, out var clip) || clip == null)
                throw new ScriptRuntimeException($"Sound '{id.FullId}' was not found. Add the asset to Sounds/ or use base:<sound> for a built-in sound.");

            return ModHelper.GetSoundIdentifier(id.ModId, id.LocalId);
        }

        internal void Apply(LuaItemDefinition definition, GameItem item)
        {
            if (!string.IsNullOrEmpty(definition.sprite))
                SetSprite(item, definition.sprite);

            LuaSoundPresets.Require(definition.sound_preset)?.Invoke(item);
            ApplySounds(definition.sounds, (slot, value) =>
            {
                switch (slot)
                {
                    case "drag_start": item.soundDragStart = value; break;
                    case "drag_end": item.soundDragEnd = value; break;
                    case "container_open": item.soundContainerOpen = value; break;
                    case "container_close": item.soundContainerClose = value; break;
                    case "select": item.soundSelect = value; break;
                    case "examine_open": item.soundExamineOpen = value; break;
                    case "toggle": item.soundToggle = value; break;
                    case "activate": item.soundActivate = value; break;
                    case "use": item.soundUse = value; break;
                }
            });
        }

        private void ApplySounds(LuaItemSounds sounds, Action<string, string?> assign)
        {
            Set("drag_start", sounds.drag_start);
            Set("drag_end", sounds.drag_end);
            Set("container_open", sounds.container_open);
            Set("container_close", sounds.container_close);
            Set("select", sounds.select);
            Set("examine_open", sounds.examine_open);
            Set("toggle", sounds.toggle);
            Set("activate", sounds.activate);
            Set("use", sounds.use);

            void Set(string slot, string? value)
            {
                if (value == null)
                    return;

                try { assign(slot, Sound(value)); }
                catch (ScriptRuntimeException exception)
                {
                    throw new ScriptRuntimeException($"sounds.{slot}: {exception.Message}");
                }
            }
        }
    }
}
