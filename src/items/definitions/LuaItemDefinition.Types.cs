using System;
using System.Collections.Generic;
using MoonSharp.Interpreter;

namespace ProbablyLua.Api
{
    public sealed partial class LuaItemDefinition
    {
        private readonly HashSet<string> _types = new(StringComparer.Ordinal);
        private readonly HashSet<string> _removedTypes = new(StringComparer.Ordinal);
        internal IEnumerable<string> Types => _types;

        // Explicit additions to the template's native itemTypes, never a live CLR list.
        public Table? types
        {
            get
            {
                var snapshot = new Table(_script);
                int index = 1;
                foreach (string type in _types)
                    snapshot.Set(index++, DynValue.NewString(type));

                return snapshot;
            }
            set
            {
                string fieldName = $"Item '{id}'.types";
                var replacement = new HashSet<string>(StringComparer.Ordinal);
                if (value != null)
                {
                    LuaValues.Array(value, fieldName);
                    for (int index = 1; index <= value.Length; index++)
                    {
                        DynValue entry = value.Get(index);
                        if (entry.Type != DataType.String)
                            throw new ScriptRuntimeException($"{fieldName}[{index}]: expected ItemType.<name> or a nonempty type string; got {entry.Type}.");

                        replacement.Add(ItemTypeApi.Require(entry.String, $"{fieldName}[{index}]"));
                    }
                }

                // Validate the entire list first: a bad entry must not partially change it.
                _types.Clear();
                _types.UnionWith(replacement);
                _removedTypes.ExceptWith(replacement);
            }
        }

        public void add_type(string type)
        {
            type = ItemTypeApi.Require(type);
            _removedTypes.Remove(type);
            _types.Add(type);
        }

        public void remove_type(string type)
        {
            type = ItemTypeApi.Require(type);
            _types.Remove(type);
            _removedTypes.Add(type);
        }

        public bool has_type(string type) => _types.Contains(ItemTypeApi.Require(type));
    }
}
