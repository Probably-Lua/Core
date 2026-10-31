using System;
using System.Globalization;
using MoonSharp.Interpreter;

namespace ProbablyLua.Api
{
    // Color is a Lua table of string constants, not CLR userdata.
    internal static class ManufacturerColors
    {
        internal static RenderHandler.ColorPalette Parse(
            string color,
            RenderHandler.ColorPalette fallback = RenderHandler.ColorPalette.LightGray)
        {
            if (string.IsNullOrEmpty(color))
                return fallback;

            if (string.Equals(color, "none", StringComparison.OrdinalIgnoreCase))
                return RenderHandler.ColorPalette.None;

            if (color.Length != 7 || color[0] != '#' ||
                !int.TryParse(color.Substring(1), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int rgb))
                throw new ScriptRuntimeException($"Invalid manufacturer color '{color}'. Use Color.<name> or a six-digit hex string such as '#FF8800'.");

            return (RenderHandler.ColorPalette)rgb;
        }

        public static void Bind(Script script)
        {
            var palette = new Table(script);
            palette.Set("None", DynValue.NewString("none"));
            palette.Set("White", DynValue.NewString("#C4C4B0"));
            palette.Set("VeryLightGray", DynValue.NewString("#A8BECF"));
            palette.Set("LightGray", DynValue.NewString("#8397BB"));
            palette.Set("Gray", DynValue.NewString("#88948A"));
            palette.Set("DarkGray", DynValue.NewString("#4F5390"));
            palette.Set("VeryDarkGray", DynValue.NewString("#413D78"));
            palette.Set("Black", DynValue.NewString("#230F47"));
            palette.Set("LightPurple", DynValue.NewString("#B082A5"));
            palette.Set("Purple", DynValue.NewString("#9D5C9D"));
            palette.Set("DarkPurple", DynValue.NewString("#80497B"));
            palette.Set("VeryLightRed", DynValue.NewString("#C3A9AE"));
            palette.Set("LightRed", DynValue.NewString("#C07D7D"));
            palette.Set("Red", DynValue.NewString("#BE514C"));
            palette.Set("DarkRed", DynValue.NewString("#904453"));
            palette.Set("VeryDarkRed", DynValue.NewString("#63375A"));
            palette.Set("LightOrange", DynValue.NewString("#B8855A"));
            palette.Set("Orange", DynValue.NewString("#AD6107"));
            palette.Set("DarkOrange", DynValue.NewString("#884C31"));
            palette.Set("LightYellow", DynValue.NewString("#93964A"));
            palette.Set("Yellow", DynValue.NewString("#877400"));
            palette.Set("DarkYellow", DynValue.NewString("#635D21"));
            palette.Set("VeryLightGreen", DynValue.NewString("#A0B994"));
            palette.Set("LightGreen", DynValue.NewString("#7A9D4A"));
            palette.Set("Green", DynValue.NewString("#548100"));
            palette.Set("DarkGreen", DynValue.NewString("#4A6421"));
            palette.Set("VeryDarkGreen", DynValue.NewString("#404741"));
            palette.Set("LightTurquoise", DynValue.NewString("#42A28B"));
            palette.Set("Turquoise", DynValue.NewString("#008952"));
            palette.Set("DarkTurquoise", DynValue.NewString("#126961"));
            palette.Set("VeryLightCyan", DynValue.NewString("#84BCC4"));
            palette.Set("LightCyan", DynValue.NewString("#42A2AA"));
            palette.Set("Cyan", DynValue.NewString("#008990"));
            palette.Set("DarkCyan", DynValue.NewString("#126980"));
            palette.Set("VeryDarkCyan", DynValue.NewString("#244970"));
            palette.Set("LightBlue", DynValue.NewString("#429CBD"));
            palette.Set("Blue", DynValue.NewString("#007CB7"));
            palette.Set("DarkBlue", DynValue.NewString("#126293"));
            palette.Set("EGreen", DynValue.NewString("#84BA51"));
            palette.Set("ERed", DynValue.NewString("#9B4A3D"));
            script.Globals.Set("Color", DynValue.NewTable(palette));
        }
    }
}
