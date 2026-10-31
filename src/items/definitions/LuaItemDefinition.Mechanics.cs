using MoonSharp.Interpreter;

namespace ProbablyLua.Api
{
    public sealed partial class LuaItemDefinition
    {
        private int? _stackLimit;
        private int? _weight;
        private int? _durability;

        public int stack_limit
        {
            get => _stackLimit ?? 1;
            set => _stackLimit = LuaValues.Range(value, 1, 1000000, $"Item '{id}'.stack_limit");
        }

        public int weight
        {
            get => _weight ?? 0;
            set => _weight = LuaValues.Range(value, 0, 1000000, $"Item '{id}'.weight");
        }

        public int durability
        {
            get => _durability ?? 0;
            set => _durability = LuaValues.Range(value, 1, 1000000, $"Item '{id}'.durability");
        }

        internal void ApplyMechanics(GameItem item)
        {
            if (LuaItemState.HasChildren(item) && (item.unitCount != 1 || _stackLimit > 1))
                throw new ScriptRuntimeException($"Item '{id}': containers and examination windows cannot be stacked.");

            if (nicotine > 0 && (_stackLimit > 1 || item.unitCount != 1))
                throw new ScriptRuntimeException($"Item '{id}': the native nicotine action consumes a whole item; use amount = 1 and stack_limit = 1.");

            if (_stackLimit.HasValue)
            {
                int limit = _stackLimit.Value;
                item.maxSlotAmountItemFunc = (_, _, _) => limit;
                item.mayStackItemFunc = (left, right) => limit > 1 && LuaItemState.CanStack(left, right);
            }

            if (_weight.HasValue)
                GeneralHelper.SetItemWeight(item, _weight.Value);

            if (_durability.HasValue)
                DurabilityHelper.InitItemWithDurability(item, _durability.Value);
        }
    }
}
