using System;
using System.Collections.Generic;
using MoonSharp.Interpreter;

namespace ProbablyLua.Api
{
    // Per-runtime interface to a shared C# translation registry.
    internal sealed class LangApi
    {
        private static readonly (string Name, string Code)[] Languages =
        {
            ("English", "en"),
            ("French", "fr"),
            ("German", "de"),
            ("Russian", "ru"),
            ("Chinese", "zh"),
            ("Japanese", "ja"),
            ("Spanish", "es"),
            ("Portuguese_Brazil", "ptBR"),
            ("Korean", "ko"),
            ("Italian", "it"),
            ("Polish", "pl"),
        };

        private readonly string _modId;
        private readonly LangRegistry _registry;
        private readonly Dictionary<string, Dictionary<string, string>> _pending = new(StringComparer.OrdinalIgnoreCase);
        private bool _committed;

        private readonly LuaCallbacks _callbacks;

        public LangApi(string modId, LangRegistry registry, LuaCallbacks callbacks)
        {
            _modId = modId;
            _registry = registry;
            _callbacks = callbacks;
        }

        public void Bind(Script script)
        {
            var constants = new Table(script);
            foreach (var (name, code) in Languages)
                constants.Set(name, DynValue.NewString(code));

            var api = new Table(script);
            api.Set("update", DynValue.NewCallback(Update));
            api.Set("get", DynValue.NewCallback(Get));

            script.Globals.Set("LangCode", DynValue.NewTable(constants));
            script.Globals.Set("lang", DynValue.NewTable(api));
        }

        private DynValue Update(ScriptExecutionContext _, CallbackArguments args)
        {

            if (_callbacks.IsReadOnly)
                throw new ScriptRuntimeException("lang.update(): translations cannot be changed from a can_target filter.");

            if (args.Count != 2 || args[0].Type != DataType.String ||
                args[1].Type != DataType.Table)
                throw new ScriptRuntimeException("lang.update(language, translations) expects a LangCode value (such as LangCode.English) and a table of strings.");

            string language = NormalizeCode(args[0].String);
            var changes = new Dictionary<string, string>(StringComparer.Ordinal);

            // Validate the complete update before applying any of its keys.
            foreach (TablePair pair in args[1].Table.Pairs)
            {
                if (pair.Key.Type != DataType.String || pair.Value.Type != DataType.String)
                    throw new ScriptRuntimeException(
                        "lang.update(language, translations): every translation key and value must be a string (example: { [\"item.soap.name\"] = \"Soap\" }).");

                if (string.IsNullOrWhiteSpace(pair.Key.String))
                    throw new ScriptRuntimeException($"lang.update('{language}', ...): translation keys must not be empty.");

                if (string.IsNullOrWhiteSpace(pair.Value.String))
                    throw new ScriptRuntimeException(
                        $"lang.update('{language}', ...): translation '{pair.Key.String}' is empty. " +
                        "Use a nonempty string or omit the key to fall back to another language.");

                changes[ResourceIds.QualifyTranslation(_modId, pair.Key.String)] = pair.Value.String;
            }

            if (_committed)
            {
                _registry.Update(language, changes);
            }
            else
            {
                if (!_pending.TryGetValue(language, out var entries))
                {
                    entries = new Dictionary<string, string>(StringComparer.Ordinal);
                    _pending.Add(language, entries);
                }

                foreach (var pair in changes)
                    entries[pair.Key] = pair.Value;
            }

            return DynValue.Nil;
        }

        private DynValue Get(ScriptExecutionContext _, CallbackArguments args)
        {
            if (args.Count < 1 ||
                args.Count > 2 ||
                args[0].Type != DataType.String ||
                (
                    args.Count == 2 &&
                    args[1].Type != DataType.String &&
                    args[1].Type != DataType.Nil &&
                    args[1].Type != DataType.Void
                ))
                throw new ScriptRuntimeException("lang.get(key, fallback?) expects a translation key string and an optional fallback string.");

            string key = args[0].String;
            if (string.IsNullOrWhiteSpace(key))
                throw new ScriptRuntimeException("lang.get(key, fallback?): key must be a nonempty string.");

            string fallback = args.Count == 2 && args[1].Type == DataType.String
                ? args[1].String : key;

            return DynValue.NewString(Resolve(key, fallback));
        }

        // Same lookup as lang.get(), also used by item and action rendering.
        public string Resolve(string key, string fallback, string? gameCategory = null, string? gameKey = null)
        {
            string fullKey = ResourceIds.QualifyTranslation(_modId, key);
            return _registry.Resolve(fullKey, fallback, gameCategory, gameKey,
                _committed ? null : LookupPending);
        }

        private string? LookupPending(string language, string fullKey)
        {
            return _pending.TryGetValue(language, out var entries) &&
                entries.TryGetValue(fullKey, out string found) ? found : null;
        }

        public void Commit()
        {
            if (_committed)
                return;

            foreach (var pair in _pending)
                _registry.Update(pair.Key, pair.Value);

            _pending.Clear();
            _committed = true;
        }

        private static string NormalizeCode(string input)
        {
            foreach (var (_, code) in Languages)
            {
                if (string.Equals(code, input, StringComparison.OrdinalIgnoreCase))
                    return code;
            }

            throw new ScriptRuntimeException($"Unsupported language code '{input}'. Use LangCode.English, LangCode.Russian, etc.");
        }
    }
}
