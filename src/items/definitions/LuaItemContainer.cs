using System;
using System.Collections.Generic;
using MoonSharp.Interpreter;

namespace ProbablyLua.Api
{
    public sealed class LuaItemContainer
    {
        public int width { get; set; }
        public int height { get; set; }
        public string lock_id { get; set; } = "";
        public string opened_sprite { get; set; } = "";

        internal bool Enabled => width != 0 || height != 0;

        internal void Validate(string itemId)
        {
            if (!Enabled)
            {
                if (!string.IsNullOrEmpty(lock_id) || !string.IsNullOrEmpty(opened_sprite))
                    throw new ScriptRuntimeException($"Item '{itemId}'.container: set width and height before configuring a lock or opened sprite.");
                    
                return;
            }

            LuaValues.Range(width, 1, 32, $"Item '{itemId}'.container.width");
            LuaValues.Range(height, 1, 32, $"Item '{itemId}'.container.height");
            if (!string.IsNullOrEmpty(lock_id))
                LuaValues.Text(lock_id, $"Item '{itemId}'.container.lock_id");
        }
    }
}
