using System;
using MoonSharp.Interpreter;
using MoonSharp.Interpreter.Interop;

namespace ProbablyLua.Api
{
    internal static class LuaUserData
    {
        internal static void Register<T>()
        {
            if (UserData.IsTypeRegistered<T>())
                return;

            // Only our small facade types are registered. Inherited CLR object
            // methods are unnecessary in Lua and must not expose reflection.
            if (UserData.RegisterType<T>() is not StandardUserDataDescriptor descriptor)
                throw new InvalidOperationException($"Cannot configure the Lua facade for {typeof(T).Name}.");

            foreach (string name in new[] { "GetType", "Equals", "GetHashCode", "ToString", "ReferenceEquals" })
                descriptor.RemoveMember(name);
        }
    }
}
