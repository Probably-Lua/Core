using System;
using System.Collections.Generic;
using HarmonyLib;

namespace ProbablyLua.Api
{
    internal sealed class ManufacturerRegistry
    {
        internal const string AdditionalTag = "PROBABLYLUA_EXTRA_MANUFACTURERS";

        private readonly LangRegistry _lang;
        private readonly Dictionary<string, LuaManufacturer> _manufacturers = new(StringComparer.OrdinalIgnoreCase);

        // Exact vanilla entries from GameItem.DisplayManufacturer().
        // "base" is a ProbablyLua namespace; vanilla game tags stay unprefixed.
        private static readonly Dictionary<string, (string LocalizationKey, RenderHandler.ColorPalette Color)>
            Vanilla = new(StringComparer.OrdinalIgnoreCase)
            {
                ["base:pastel"] = ("corpo_name_pastel", RenderHandler.ColorPalette.VeryLightGreen),
                ["base:carmicheal"] = ("corpo_name_carmicheal", RenderHandler.ColorPalette.LightTurquoise),
                ["base:davre"] = ("corpo_name_davre", RenderHandler.ColorPalette.Green),
                ["base:mnsl"] = ("corpo_name_mnsl", RenderHandler.ColorPalette.Red),
                ["base:eminence"] = ("corpo_name_eminence", RenderHandler.ColorPalette.LightYellow),
                ["base:eninence"] = ("corpo_name_eninence", RenderHandler.ColorPalette.LightYellow),
                ["base:republic"] = ("corpo_name_republic", RenderHandler.ColorPalette.LightBlue),
                ["base:republic_navy"] = ("corpo_name_republic_navy", RenderHandler.ColorPalette.Blue),
                ["base:neural"] = ("corpo_name_neural", RenderHandler.ColorPalette.VeryLightGray),
                ["base:watch"] = ("corpo_name_watch", RenderHandler.ColorPalette.Black),
                ["base:front"] = ("corpo_name_front", RenderHandler.ColorPalette.LightRed),
                ["base:sokolov"] = ("corpo_name_sokolov", RenderHandler.ColorPalette.Red),
                ["base:vision"] = ("corpo_name_vision", RenderHandler.ColorPalette.LightBlue),
                ["base:zhonghua"] = ("corpo_name_zhonghua", RenderHandler.ColorPalette.LightRed),
                ["base:spacer"] = ("corpo_name_spacer", RenderHandler.ColorPalette.LightGray),
                ["base:meridian"] = ("corpo_name_meridian", RenderHandler.ColorPalette.LightRed),
                ["base:echelon"] = ("corpo_name_echelon", RenderHandler.ColorPalette.LightBlue),
                ["base:blueshield"] = ("corpo_name_blueshield", RenderHandler.ColorPalette.Blue),
                ["base:frontier"] = ("corpo_name_frontier", RenderHandler.ColorPalette.Green),
                ["base:ashvins"] = ("corpo_name_ashvins", RenderHandler.ColorPalette.Green),
                ["base:solana"] = ("corpo_name_solana", RenderHandler.ColorPalette.Turquoise),
                ["base:alison"] = ("corpo_name_alison", RenderHandler.ColorPalette.LightBlue),
                ["base:harvey_bee"] = ("ClientName", RenderHandler.ColorPalette.LightBlue),
                ["base:georgio"] = ("name_georgio", RenderHandler.ColorPalette.Red),
            };

        public ManufacturerRegistry(LangRegistry lang)
        {
            _lang = lang;
        }

        public void Register(IEnumerable<LuaManufacturer> definitions)
        {
            foreach (LuaManufacturer definition in definitions)
                _manufacturers[definition.id] = definition; // Last registered wins.
        }

        public void Render(GameItem item, RichTextBuilder builder, string manufacturer)
        {
            RenderOne(builder, manufacturer);

            if (!item.IsTag(AdditionalTag))
                return;

            string extras = item.GetTagReadonly(AdditionalTag).GetString();
            if (string.IsNullOrEmpty(extras))
                return;

            foreach (string name in extras.Split('\n'))
                RenderOne(builder, name);
        }

        private void RenderOne(RichTextBuilder builder, string manufacturer)
        {
            if (string.IsNullOrWhiteSpace(manufacturer))
                return;

            ResourceId resource = ResourceIds.FromManufacturerTag(manufacturer);
            string id = resource.FullId;
            string key = ResourceIds.ManufacturerNameKey(resource);

            bool vanilla = Vanilla.TryGetValue(id, out var builtIn);
            // Lookup in the game happens in the centralized lang resolver,
            // after the current-language Lua overrides and before Lua English.
            string defaultName = string.Equals(id, "base:harvey_bee",
                StringComparison.OrdinalIgnoreCase) ? "M. Harvey Bee" :
                (resource.IsBase ? resource.LocalId : manufacturer);

            string? gameKey = vanilla ? (id.Equals("base:harvey_bee",
                StringComparison.OrdinalIgnoreCase) ? "corpo_name_harvey_bee" :
                builtIn.LocalizationKey) : null;

            RenderHandler.ColorPalette defaultColor = vanilla
                ? builtIn.Color
                : RenderHandler.ColorPalette.LightGray;

            string name;
            RenderHandler.ColorPalette color;
            bool bold = false;
            bool italic = false;
            bool underline = true;

            if (_manufacturers.TryGetValue(id, out LuaManufacturer definition))
            {
                string fallback = string.IsNullOrWhiteSpace(definition.name)
                    ? (vanilla ? defaultName : definition.LocalId)
                    : definition.name;

                name = _lang.Resolve(key, fallback, gameKey == null ? null : "ClientName", gameKey);
                color = ManufacturerColors.Parse(definition.color, defaultColor);
                bold = definition.bold;
                italic = definition.italic;
                underline = definition.underline;
            }
            else
            {
                name = _lang.Resolve(key, defaultName,
                    gameKey == null ? null : "ClientName", gameKey);

                color = defaultColor;
            }

            builder.Add(" ");
            builder.AddLine(name, ignoreEmpty: true, color,
                bold: bold, italic: italic, underline: underline);
        }

        public void Clear() => _manufacturers.Clear();
    }
}
