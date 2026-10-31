using System;
using System.Collections.Generic;
using MoonSharp.Interpreter;

namespace ProbablyLua.Api
{
    public sealed partial class LuaItemDefinition
    {
        // Accepts a vanilla/custom string ID, or a LuaManufacturer userdata.
        private object? _manufacturer;
        public object? manufacturer
        {
            get => _manufacturer;
            set
            {
                if (value is string name && string.IsNullOrWhiteSpace(name))
                    throw new ScriptRuntimeException($"Item '{id}': manufacturer must not be empty. Use nil to remove it.");

                if (value != null && value is not string && value is not LuaManufacturer)
                    throw new ScriptRuntimeException(
                        $"Item '{id}': manufacturer expects a string ID or " +
                        $"factory.manufacturer() reference, got {value.GetType().Name}.");

                _manufacturer = value;
            }
        }

        // The game stores one primary manufacturer. Additional manufacturers
        // are a ProbablyLua extension, rendered from a separate item tag.
        private Table? _manufacturers;
        public Table? manufacturers
        {
            get => _manufacturers;
            set
            {
                if (value != null)
                    ValidateManufacturers(value);

                _manufacturers = value;
            }
        }

        internal IEnumerable<object> AdditionalManufacturers
        {
            get
            {
                Table? entries = _manufacturers;
                if (entries == null)
                    yield break;

                // Validate again when spawning: Lua tables are mutable.
                ValidateManufacturers(entries);
                for (int i = 1; i <= entries.Length; i++)
                    yield return ManufacturerFromLua(entries.Get(i));
            }
        }

        private void ValidateManufacturers(Table table)
        {
            int count = 0;
            foreach (TablePair pair in table.Pairs)
            {
                if (pair.Key.Type != DataType.Number ||
                    pair.Key.Number != Math.Floor(pair.Key.Number) ||
                    pair.Key.Number < 1 || pair.Key.Number > table.Length)
                    throw new ScriptRuntimeException(
                        $"Item '{id}': manufacturers must be a Lua array " +
                        "(example: { 'base:pastel', my_manufacturer }).");

                ManufacturerFromLua(pair.Value);
                count++;
            }

            if (count != table.Length)
                throw new ScriptRuntimeException($"Item '{id}': manufacturers must be a sequence without gaps.");
        }

        private object ManufacturerFromLua(DynValue value)
        {
            if (value.Type == DataType.String && !string.IsNullOrWhiteSpace(value.String))
                return value.String;

            if (value.Type == DataType.UserData && value.UserData.Object is LuaManufacturer manufacturer)
                return manufacturer;

            throw new ScriptRuntimeException($"Item '{id}': manufacturers entries must be nonempty string IDs or values returned by factory.manufacturer(), got {value.Type}.");
        }
    }
}
