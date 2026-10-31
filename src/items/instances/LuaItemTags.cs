using System;
using MoonSharp.Interpreter;

namespace ProbablyLua.Api
{
    internal static class LuaItemTags
    {
        internal static void Validate(string key, DynValue value)
        {
            LuaValues.Text(key, "Tag ID");
            if (LuaValues.IsNil(value) || value.Type == DataType.String || value.Type == DataType.Boolean)
                return;

            if (value.Type == DataType.Number && !double.IsNaN(value.Number) &&
                !double.IsInfinity(value.Number) && Math.Abs(value.Number) <= 9007199254740991d)
                return;

            throw new ScriptRuntimeException($"Tag '{key}': expected nil, boolean, string or a finite number within Lua's exact integer range.");
        }

        internal static void Apply(GameItem item, string key, DynValue value)
        {
            Validate(key, value);
            if (LuaValues.IsNil(value))
            {
                item.DisableTag(key);
                return;
            }

            item.ModifyTag(key, state =>
            {
                state.Enable();
                switch (value.Type)
                {
                    case DataType.String:
                        state.SetString(value.String);
                        break;

                    case DataType.Boolean:
                        state.SetBool(value.Boolean);
                        break;

                    case DataType.Number:
                        state.SetDouble(value.Number).SetFloat((float)value.Number);
                        if (value.Number == Math.Truncate(value.Number))
                        {
                            state.SetLong((long)value.Number);
                            if (value.Number >= int.MinValue && value.Number <= int.MaxValue)
                                state.SetInt((int)value.Number);
                        }
                        break;
                }
            });
        }
    }
}
