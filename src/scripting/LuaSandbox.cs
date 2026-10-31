using MoonSharp.Interpreter;

namespace ProbablyLua.Api
{
    internal static class LuaSandbox
    {
        internal static Script Create()
        {
            var script = new Script(CoreModules.Preset_HardSandbox);
            foreach (string name in new[]
            {
                "io", "os", "debug", "package", "require", "load", "loadfile",
                "loadstring", "collectgarbage", "coroutine", "luanet", "clr"
            })
                script.Globals.Set(name, DynValue.Nil);

            return script;
        }
    }
}
