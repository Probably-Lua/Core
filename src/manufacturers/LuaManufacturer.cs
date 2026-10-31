using System;
using System.Collections.Generic;
using MoonSharp.Interpreter;

namespace ProbablyLua.Api
{
    public sealed class LuaManufacturer
    {
        public string id { get; }
        public string name { get; set; } = "";

        private string _color = "";
        public string color
        {
            get => _color;
            set
            {
                // Reject invalid colors while the Lua script is running,
                // not later from inside the game's tooltip renderer.
                ManufacturerColors.Parse(value);
                _color = value ?? "";
            }
        }
        public bool bold { get; set; }
        public bool italic { get; set; }
        public bool underline { get; set; } = true;

        internal string LocalId { get; }
        internal string OwnerModId { get; }

        internal LuaManufacturer(ResourceId resource)
        {
            OwnerModId = resource.ModId;
            LocalId = resource.LocalId;
            id = resource.FullId;
        }
    }
}
