using System;
using System.Collections.Generic;
using MoonSharp.Interpreter;

namespace ProbablyLua.Api
{
    public sealed partial class LuaItemDefinition
    {
        private DynValue? _onSpawn;
        private DynValue? _onLoaded;
        private DynValue? _onSecond;
        private DynValue? _onTurn;
        private DynValue? _onCycle;
        private DynValue? _onAdded;
        private DynValue? _onRemoved;
        private DynValue? _onAmountChanged;
        private DynValue? _onUnlocked;

        public DynValue? on_added
        {
            get => _onAdded;
            set => _onAdded = LuaValues.OptionalFunction(value, $"Item '{id}'.on_added");
        }

        public DynValue? on_removed
        {
            get => _onRemoved;
            set => _onRemoved = LuaValues.OptionalFunction(value, $"Item '{id}'.on_removed");
        }

        public DynValue? on_amount_changed
        {
            get => _onAmountChanged;
            set => _onAmountChanged = LuaValues.OptionalFunction(value, $"Item '{id}'.on_amount_changed");
        }

        public DynValue? on_unlocked
        {
            get => _onUnlocked;
            set => _onUnlocked = LuaValues.OptionalFunction(value, $"Item '{id}'.on_unlocked");
        }

        public DynValue? on_spawn
        {
            get => _onSpawn;
            set => _onSpawn = LuaValues.OptionalFunction(value, $"Item '{id}'.on_spawn");
        }

        public DynValue? on_loaded
        {
            get => _onLoaded;
            set => _onLoaded = LuaValues.OptionalFunction(value, $"Item '{id}'.on_loaded");
        }

        public DynValue? on_second
        {
            get => _onSecond;
            set => _onSecond = LuaValues.OptionalFunction(value, $"Item '{id}'.on_second");
        }

        public DynValue? on_turn
        {
            get => _onTurn;
            set => _onTurn = LuaValues.OptionalFunction(value, $"Item '{id}'.on_turn");
        }

        public DynValue? on_cycle
        {
            get => _onCycle;
            set => _onCycle = LuaValues.OptionalFunction(value, $"Item '{id}'.on_cycle");
        }
    }
}
