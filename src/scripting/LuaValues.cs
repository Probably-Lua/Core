using System;
using MoonSharp.Interpreter;

namespace ProbablyLua.Api
{
    internal static class LuaValues
    {
        internal static bool IsNil(DynValue? value) =>
            value == null || value.Type == DataType.Nil || value.Type == DataType.Void;

        internal static DynValue? OptionalFunction(DynValue? value, string field)
        {
            if (IsNil(value))
                return null;

            if (value!.Type != DataType.Function)
                throw new ScriptRuntimeException($"{field}: expected a Lua function or nil; got {value.Type}.");

            return value;
        }

        internal static string Text(string value, string field)
        {
            if (string.IsNullOrWhiteSpace(value) || value != value.Trim())
                throw new ScriptRuntimeException($"{field}: expected a nonempty string without surrounding whitespace.");

            foreach (char c in value)
                if (char.IsControl(c))
                    throw new ScriptRuntimeException($"{field}: control characters are not allowed.");

            return value;
        }

        internal static int Range(int value, int min, int max, string field)
        {
            if (value < min || value > max)
                throw new ScriptRuntimeException($"{field}: expected an integer from {min} to {max}; got {value}.");

            return value;
        }

        internal static int Integer(DynValue value, int min, int max, string field)
        {
            if (value.Type != DataType.Number)
                throw new ScriptRuntimeException($"{field}: expected an integer from {min} to {max}.");

            return Integer(value.Number, min, max, field);
        }

        internal static int Integer(double value, int min, int max, string field)
        {
            if (double.IsNaN(value) || value < min || value > max || value != Math.Floor(value))
                throw new ScriptRuntimeException($"{field}: expected an integer from {min} to {max}.");

            return (int)value;
        }

        internal static void Array(Table table, string field)
        {
            int count = 0;
            foreach (TablePair pair in table.Pairs)
            {
                if (pair.Key.Type != DataType.Number || pair.Key.Number != Math.Floor(pair.Key.Number) ||
                    pair.Key.Number < 1 || pair.Key.Number > table.Length)
                    throw new ScriptRuntimeException($"{field}: expected an array with consecutive indices starting at 1.");

                count++;
            }

            if (count != table.Length)
                throw new ScriptRuntimeException($"{field}: array indices must not contain gaps.");
        }
    }
}
