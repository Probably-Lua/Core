using System;
using MoonSharp.Interpreter;

namespace ProbablyLua.Api
{
    internal readonly struct TranslationId
    {
        public ResourceId Resource { get; }
        public string Type { get; }
        public string Field { get; }
        public string Path => ResourceIds.TranslationPath(Type, Resource.LocalId, Field);
        public string FullKey => Resource.ModId + ":" + Path;

        public TranslationId(ResourceId resource, string type, string field)
        {
            Resource = resource;
            Type = type;
            Field = field;
        }
    }
}
