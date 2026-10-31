using System;
using MoonSharp.Interpreter;

namespace ProbablyLua.Api
{
    // All resource IDs and translation keys are parsed and formatted here.
    internal readonly struct ResourceId
    {
        public string ModId { get; }
        public string LocalId { get; }
        public string FullId => ModId + ":" + LocalId;
        public bool IsBase => string.Equals(ModId, ResourceIds.BaseModId,
            StringComparison.OrdinalIgnoreCase);

        public ResourceId(string modId, string localId)
        {
            ModId = modId;
            LocalId = localId;
        }
    }
}
