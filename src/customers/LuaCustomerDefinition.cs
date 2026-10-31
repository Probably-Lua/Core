using System;
using System.Collections.Generic;
using MoonSharp.Interpreter;

namespace ProbablyLua.Api
{
    public sealed class LuaCustomerDefinition
    {
        public string id { get; }
        public string name { get; set; } = "";
        public string faction { get; set; } = "FACTION_BLACK_MARKET";
        public string sprite { get; set; } = "random:merc";
        public Table? dialogue { get; set; }
        private DynValue? _onTrade;
        public DynValue? on_trade
        {
            get => _onTrade;
            set => _onTrade = LuaValues.OptionalFunction(value, $"Customer '{id}'.on_trade");
        }

        internal LuaCustomerDefinition(string id) => this.id = id;

        internal void Validate()
        {
            LuaValues.Text(name, $"Customer '{id}'.name");
            LuaValues.Text(faction, $"Customer '{id}'.faction");
            LuaValues.Text(sprite, $"Customer '{id}'.sprite");
            if (dialogue == null || dialogue.Length == 0)
                throw new ScriptRuntimeException($"Customer '{id}'.dialogue: expected at least one dialogue line.");

            LuaValues.Array(dialogue, $"Customer '{id}'.dialogue");
            for (int i = 1; i <= dialogue.Length; i++)
                if (dialogue.Get(i).Type != DataType.String)
                    throw new ScriptRuntimeException($"Customer '{id}'.dialogue[{i}]: expected a string.");
        }
    }
}
