namespace ProbablyLua
{
    // Framework-only preferences; Lua mods do not receive this API.
    internal static class ProbablyLuaSettings
    {
        private static ConfigEntry<bool>? _hideUnsupportedContextActions;

        internal static bool HideUnsupportedContextActions =>
            _hideUnsupportedContextActions?.Value ?? true;

        internal static void Initialize(string modId)
        {
            ModConfig config = ModHelper.GetConfig(modId);

            _hideUnsupportedContextActions = config.Define(
                "hide_unsupported_context_actions",
                true,
                "Context menu",
                () => "Hide unsupported item actions",
                tooltip: () =>
                    "Remove context-menu actions that the selected item does not support. " +
                    "Temporarily unavailable actions remain visible."
            );
        }
    }
}
