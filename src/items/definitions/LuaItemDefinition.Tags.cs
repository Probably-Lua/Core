using System;
using System.Collections.Generic;
using MoonSharp.Interpreter;

namespace ProbablyLua.Api
{
    public sealed partial class LuaItemDefinition
    {
        private readonly Dictionary<string, DynValue> _tags = new(StringComparer.Ordinal);

        public void set_tag(string key, DynValue value)
        {
            LuaItemTags.Validate(key, value);
            _tags[key] = value;
        }

        public void remove_tag(string key) => _tags[LuaValues.Text(key, "Tag ID")] = DynValue.Nil;
    }
}
