using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

namespace ProbablyLua.Api
{
    // One registry for the entire Lua host; individual scripts keep separate globals.
    internal sealed class LangRegistry
    {
        private readonly Dictionary<string, Dictionary<string, string>> _translations =
            new(StringComparer.OrdinalIgnoreCase);

        // Global item-type names are shared between mods; the last registered
        // translation for a type wins, just as other translations do.
        private readonly Dictionary<string, Dictionary<string, string>> _typeNames = new(StringComparer.OrdinalIgnoreCase);

        private static readonly HashSet<string> GameLookupWarnings = new(StringComparer.OrdinalIgnoreCase);

        public void Update(string language, IReadOnlyDictionary<string, string> translations)
        {
            if (!_translations.TryGetValue(language, out var entries))
            {
                entries = new Dictionary<string, string>(StringComparer.Ordinal);
                _translations.Add(language, entries);
            }

            foreach (var pair in translations)
            {
                entries[pair.Key] = pair.Value; // Last applied entry wins.
                int separator = pair.Key.IndexOf(':');
                string local = separator < 0 ? "" : pair.Key.Substring(separator + 1);
                if (local.StartsWith("type.", StringComparison.OrdinalIgnoreCase) &&
                    local.EndsWith(".name", StringComparison.OrdinalIgnoreCase))
                {
                    string type = local.Substring(5, local.Length - 10);
                    if (!_typeNames.TryGetValue(language, out var types))
                    {
                        types = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                        _typeNames[language] = types;
                    }
                    types[type] = pair.Value;
                }
            }
        }

        public bool TryGet(string language, string key, out string translation)
        {
            if (_translations.TryGetValue(language, out var entries) &&
                entries.TryGetValue(key, out var found))
            {
                translation = found;
                return true;
            }

            translation = "";
            return false;
        }

        // One resolver for both Lua lang.get() and C# renderers. Pending Lua
        // translations are consulted by LangApi before commit via the same
        // five-step order, not by a separate fallback sequence.
        public string Resolve(
            string fullKey,
            string fallback,
            string? gameCategory = null,
            string? gameKey = null,
            Func<string, string, string?>? pending = null)
        {
            string current = ModHelper.GetCurrentLocale()?.Identifier.Code ?? "en";
            if (gameCategory == null && gameKey == null)
                GetVanillaLookup(fullKey, out gameCategory, out gameKey);

            if (TryLua(current, fullKey, pending, out string translation))
                return translation;

            if (TryNative(current, fullKey, out translation) ||
                TryGame(current, gameCategory, gameKey, out translation))
                return translation;

            if (!string.Equals(current, "en", StringComparison.OrdinalIgnoreCase))
            {
                if (TryLua("en", fullKey, pending, out translation))
                    return translation;

                if (TryNative("en", fullKey, out translation) ||
                    TryGame("en", gameCategory, gameKey, out translation))
                    return translation;
            }
            return fallback;
        }

        private bool TryLua(
            string language,
            string key,
            Func<string, string, string?>? pending,
            out string text)
        {
            string? local = pending?.Invoke(language, key);
            if (IsTranslation(local))
            {
                text = local!;
                return true;
            }

            if (TryGet(language, key, out string registered) && IsTranslation(registered))
            {
                text = registered;
                return true;
            }

            // TypeHelper only receives the type string, not the defining mod.
            // Let a type translation declared by any mod be found by its name.
            TranslationId id = ResourceIds.ParseFullTranslation(key);
            if (id.Resource.IsBase &&
                string.Equals(id.Type, "type", StringComparison.OrdinalIgnoreCase) &&
                string.Equals(id.Field, "name", StringComparison.OrdinalIgnoreCase) &&
                _typeNames.TryGetValue(language, out var types) &&
                types.TryGetValue(id.Resource.LocalId, out string typeName) &&
                IsTranslation(typeName))
            {
                text = typeName;
                return true;
            }
            text = "";
            return false;
        }

        // Recognize vanilla keys even when Lua calls lang.get() directly.
        // Explicit gameCategory/gameKey arguments are only needed for special
        // display cases (for instance manufacturer aliases).
        private static void GetVanillaLookup(string fullKey, out string? category, out string? key)
        {
            category = null;
            key = null;
            TranslationId id = ResourceIds.ParseFullTranslation(fullKey);
            if (string.Equals(id.Type, "item", StringComparison.OrdinalIgnoreCase) && id.Resource.IsBase)
            {
                category = "Item";
                string field = string.Equals(id.Field, "description",
                    StringComparison.OrdinalIgnoreCase) ? "desc" :
                    (string.Equals(id.Field, "flavor", StringComparison.OrdinalIgnoreCase)
                        ? "flavor" : id.Field);

                key = "item_" + id.Resource.LocalId + "_" + field;
                return;
            }

            if (!string.Equals(id.Field, "name", StringComparison.OrdinalIgnoreCase))
                return;

            if (string.Equals(id.Type, "type", StringComparison.OrdinalIgnoreCase))
            {
                category = "Mechanic";
                key = "type_" + id.Resource.LocalId.ToLowerInvariant();
            }
            else if (id.Resource.IsBase &&
                    string.Equals(id.Type, "action", StringComparison.OrdinalIgnoreCase) &&
                    ResourceIds.IsVanillaAction("base:" + id.Resource.LocalId))
            {
                category = "Mechanic";
                key = "handler_btn_" + id.Resource.LocalId.ToLowerInvariant();
            }
            else if (id.Resource.IsBase &&
                string.Equals(id.Type, "manufacturer", StringComparison.OrdinalIgnoreCase))
            {
                category = "ClientName";
                key = "corpo_name_" + id.Resource.LocalId;
            }
        }

        // Query an explicit Unity locale; never mutate SelectedLocale. Unity's
        // normal project fallback is disabled so that *our* English translation
        // gets a chance before the game's English translation.
        private static bool TryGame(string language, string? category, string? key, out string text)
        {
            text = "";
            if (string.IsNullOrWhiteSpace(category) || string.IsNullOrWhiteSpace(key))
                return false;

            try
            {
                var database = LocalizationSettings.StringDatabase;
                Locale? locale = LocalizationSettings.AvailableLocales?.GetLocale(language);
                if (database == null || locale == null)
                    return false;

                var table = database.GetTable(category, locale);
                var entry = table?.GetEntry(key);
                if (entry == null || !IsTranslation(entry.Value))
                    return false;

                string value = database.GetLocalizedString(
                    category, key, locale, FallbackBehavior.DontUseFallback);
                if (!IsTranslation(value))
                    return false;

                text = value;
                return true;
            }
            catch (Exception exception)
            {
                // Do not break the menu/tooltip due to a missing or unloaded
                // localization table. Unexpected failures are reported once.
                string warningId = language + ":" + category + "/" + key;
                if (GameLookupWarnings.Add(warningId))
                    Debug.LogWarning($"[ProbablyLua] Could not read game localization '{category}/{key}' for locale '{language}': {exception.Message}");

                return false;
            }
        }

        internal static bool IsTranslation(string? value)
        {
            return !string.IsNullOrWhiteSpace(value) &&
                !value.StartsWith("Translation Error", StringComparison.OrdinalIgnoreCase);
        }

        // The game already parses mod localization files during mod loading.
        // Read its loaded dictionaries directly so we can request an exact
        // language. ModHelper.GetLocalized() has its own English fallback,
        // which would bypass the Lua translations in our fallback chain.
        private static bool TryNative(string language, string fullKey, out string translation)
        {
            translation = "";
            TranslationId id = ResourceIds.ParseFullTranslation(fullKey);
            if (id.Resource.IsBase)
                return false;

            LoadedMod? mod = ModLoader.Mods.Find(m =>
                string.Equals(m.Manifest.ID, id.Resource.ModId,
                    StringComparison.OrdinalIgnoreCase));

            if (mod == null || !mod.Localization.TryGetValue(language, out var entries))
                return false;

            if (entries.TryGetValue(id.Path, out string value) && IsTranslation(value))
            {
                translation = value;
                return true;
            }

            return false;
        }

        public void Clear()
        {
            _translations.Clear();
            _typeNames.Clear();
            GameLookupWarnings.Clear();
        }
    }
}
