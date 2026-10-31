using System;
using MoonSharp.Interpreter;

namespace ProbablyLua.Api
{
    internal static class ResourceIds
    {
        public const string BaseModId = "base";

        // Lua ItemAction values and raw strings use the same string IDs.
        internal static readonly string[] VanillaActionIds =
        {
            "base:open", "base:use", "base:equip", "base:activate",
            "base:toggle", "base:unload", "base:pin"
        };

        public static bool IsVanillaAction(string qualifiedId)
        {
            foreach (string id in VanillaActionIds)
                if (string.Equals(id, qualifiedId, StringComparison.OrdinalIgnoreCase))
                    return true;

            return false;
        }

        public static string ItemActionId(string currentModId, string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                throw new ScriptRuntimeException("Action ID cannot be empty.");

            ResourceId parsed = Parse(input, currentModId);
            if (parsed.IsBase && !IsVanillaAction(parsed.FullId))
                throw new ScriptRuntimeException($"Unknown vanilla action: '{input}'.");

            return parsed.FullId;
        }

        public static string RequireActionId(string input)
        {
            RequireQualified(input);
            ResourceId id = Parse(input, BaseModId);
            if (id.IsBase && !IsVanillaAction(id.FullId))
                throw new ArgumentException($"Unknown vanilla action: '{input}'.", nameof(input));

            return id.FullId;
        }

        public static string ActionNameKey(string id)
        {
            ResourceId resource = Parse(id, BaseModId);
            return resource.ModId + ":" + TranslationPath("action", resource.LocalId, "name");
        }

        public static string ActionLabelFallback(string id) =>
            Parse(id, BaseModId).LocalId;

        // Shared validation for C# extension IDs; never parse identifiers in
        // individual registries. A fully qualified modId:localId is required.
        public static string RequireQualified(string input)
        {
            if (string.IsNullOrWhiteSpace(input) || input.IndexOf(':') < 1)
                throw new ArgumentException("Expected a qualified resource ID (modId:localId).", nameof(input));

            return Parse(input, BaseModId).FullId;
        }

        // Lua-defined actions belong exclusively to their defining mod.
        public static ResourceId NewContextAction(string modId, string localId)
        {
            ValidatePart(modId, "Mod ID");
            ValidatePart(localId, "Context action ID");
            return new ResourceId(modId, localId);
        }

        // factory.item() only creates resources owned by its Lua mod.
        public static ResourceId NewItem(string modId, string localId)
        {
            ValidatePart(modId, "Mod ID");
            ValidatePart(localId, "Item ID");
            return new ResourceId(modId, localId);
        }

        // factory.manufacturer() can additionally override vanilla base: entries.
        public static ResourceId NewManufacturer(string modId, string input)
        {
            ResourceId id = Parse(input, modId);
            if (!id.IsBase && !string.Equals(id.ModId, modId, StringComparison.OrdinalIgnoreCase))
                throw new ScriptRuntimeException("factory.manufacturer() can only define local or base: manufacturers.");

            return id;
        }

        // Bare references are always local. Vanilla resources require base:.
        public static string ToManufacturerTag(string currentModId, string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return "";

            ResourceId id = Parse(input, currentModId);
            return id.IsBase ? id.LocalId : id.FullId;
        }

        public static ResourceId FromManufacturerTag(string tag) =>
            Parse(tag, BaseModId);

        public static string ManufacturerNameKey(ResourceId manufacturer) =>
            new TranslationId(manufacturer, "manufacturer", "name").FullKey;

        public static string TranslationPath(string type, string localId, string field)
        {
            ValidatePart(type, "Resource type", allowDot: false);
            ValidatePart(localId, "Resource ID");
            ValidatePart(field, "Translation field", allowDot: false);
            return type + "." + localId + "." + field;
        }

        public static string QualifyTranslation(string currentModId, string key) =>
            ParseTranslation(currentModId, key).FullKey;

        public static TranslationId ParseFullTranslation(string fullKey)
        {
            if (fullKey.IndexOf(':') < 1)
                throw new ScriptRuntimeException($"Expected a qualified translation key: '{fullKey}'.");

            return ParseTranslation(BaseModId, fullKey);
        }

        private static TranslationId ParseTranslation(string currentModId, string input)
        {
            ResourceId raw = Parse(input, currentModId);
            int first = raw.LocalId.IndexOf('.');
            int last = raw.LocalId.LastIndexOf('.');
            if (first <= 0 || last <= first + 1 || last == raw.LocalId.Length - 1)
                throw new ScriptRuntimeException($"Translation key '{input}' must use type.id.field, for example 'item.soap.name'.");

            string type = raw.LocalId.Substring(0, first);
            string name = raw.LocalId.Substring(first + 1, last - first - 1);
            string field = raw.LocalId.Substring(last + 1);
            return new TranslationId(new ResourceId(raw.ModId, name), type, field);
        }

        internal static ResourceId Parse(string input, string defaultModId)
        {
            if (string.IsNullOrWhiteSpace(input))
                throw new ScriptRuntimeException("Resource ID cannot be empty.");

            int colon = input.IndexOf(':');
            string modId = colon < 0 ? defaultModId : input.Substring(0, colon);
            string localId = colon < 0 ? input : input.Substring(colon + 1);
            ValidatePart(modId, "Mod ID");
            ValidatePart(localId, "Resource ID");
            if (string.Equals(modId, BaseModId, StringComparison.OrdinalIgnoreCase))
                modId = BaseModId;

            return new ResourceId(modId, localId);
        }

        private static void ValidatePart(string value, string name, bool allowDot = true)
        {
            if (string.IsNullOrWhiteSpace(value) || value.Contains(":") ||
                value.Trim() != value || (!allowDot && value.Contains(".")))
                throw new ScriptRuntimeException(
                    $"Invalid {name} '{value ?? "nil"}': expected a nonempty ID without ':' or surrounding whitespace.");

            foreach (char c in value)
                if (char.IsControl(c))
                    throw new ScriptRuntimeException(
                        $"Invalid {name}: control characters are not allowed in IDs.");
        }
    }
}
