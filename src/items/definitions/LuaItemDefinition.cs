using System;
using System.Collections.Generic;
using MoonSharp.Interpreter;

namespace ProbablyLua.Api
{
    public sealed partial class LuaItemDefinition
    {
        public string id { get; }

        private readonly string _modId;
        private readonly Script _script;

        internal string LocalId { get; }

        internal LuaItemDefinition(string modId, string id, string localId,
            LuaActionCatalog actionTypes, Script script)
        {
            _modId = modId;
            _script = script;
            _actionTypes = actionTypes;
            this.id = id;
            LocalId = localId;
        }
    }
}
