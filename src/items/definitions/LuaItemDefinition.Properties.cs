using System;
using System.Collections.Generic;
using MoonSharp.Interpreter;

namespace ProbablyLua.Api
{
    public sealed partial class LuaItemDefinition
    {
        public string name { get; set; } = "";
        public string sprite { get; set; } = "";
        public string description { get; set; } = "";
        public string flavor { get; set; } = "";
        private long? _value;
        public long value
        {
            get => _value ?? 0;
            set
            {
                if (value < 0 || value > 9007199254740991L)
                    throw new ScriptRuntimeException($"Item '{id}'.value: expected a nonnegative integer no greater than 2^53 - 1.");
                _value = value;
            }
        }
        public LuaItemSounds sounds { get; } = new LuaItemSounds();

        private ContrabandLevelKind _contrabandLevel = ContrabandLevelKind.None;
        internal bool HasContrabandLevel { get; private set; }
        public string contraband_level
        {
            get => ContrabandLevelApi.ToGameValue(_contrabandLevel);
            set
            {
                _contrabandLevel = ContrabandLevelApi.Require(value);
                HasContrabandLevel = true;
            }
        }
        internal ContrabandLevelKind ContrabandLevel => _contrabandLevel;

        public string? base_item { get; internal set; }
        public string long_description { get; set; } = "";
        public string sound_preset { get; set; } = "";
        public LuaItemContainer container { get; } = new();

        private int? _amount;
        public int amount
        {
            get => _amount ?? 1;
            set => _amount = LuaValues.Range(value, 1, 1000000, $"Item '{id}'.amount");
        }

        private int _nicotine;
        public int nicotine
        {
            get => _nicotine;
            set => _nicotine = LuaValues.Range(value, 0, 100, $"Item '{id}'.nicotine");
        }

        internal void ApplyProperties(GameItem item)
        {
            if (_value.HasValue)
                item.SetValue(_value.Value);

            if (_amount.HasValue || base_item == null)
                item.SetAmount(amount);

            foreach (string type in _removedTypes)
                item.RemoveGameItemType(type);

            foreach (string type in _types)
                item.SetGameItemType(type);

            foreach (var pair in _tags)
                LuaItemTags.Apply(item, pair.Key, pair.Value);
        }

        internal void Validate()
        {
            container.Validate(id);
            LuaSoundPresets.Require(sound_preset);
        }
    }
}
