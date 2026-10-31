using System;
using System.Collections.Generic;
using MoonSharp.Interpreter;

namespace ProbablyLua.Api
{
    internal sealed class LuaManufacturerFactory
    {
        private readonly string _modId;
        private bool _sealed;
        private readonly Dictionary<string, LuaManufacturer> _definitions =
            new(StringComparer.OrdinalIgnoreCase);

        public IEnumerable<LuaManufacturer> Definitions => _definitions.Values;

        public LuaManufacturerFactory(string modId)
        {
            _modId = modId;
        }

        public void Bind(Table factory)
        {
            LuaUserData.Register<LuaManufacturer>();

            factory.Set("manufacturer", DynValue.NewCallback((_, args) =>
            {
                if (_sealed)
                    throw new ScriptRuntimeException("factory.manufacturer() is only available during Lua initialization.");

                if (args.Count != 1 || args[0].Type != DataType.String)
                    throw new ScriptRuntimeException(
                        "factory.manufacturer(id) expects a string ID.");

                ResourceId resource = ResourceIds.NewManufacturer(_modId, args[0].String);
                string fullId = resource.FullId;
                if (_definitions.ContainsKey(fullId))
                    throw new ScriptRuntimeException(
                        $"Manufacturer '{fullId}' already exists.");

                var manufacturer = new LuaManufacturer(resource);
                _definitions.Add(fullId, manufacturer);
                return UserData.Create(manufacturer);
            }));
        }

        internal void Seal() => _sealed = true;
    }
}
