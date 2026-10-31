using System;
using System.Collections.Generic;
using MoonSharp.Interpreter;

namespace ProbablyLua.Api
{
    internal sealed class LuaActionBinding
    {
        internal readonly DynValue Callback;
        internal readonly DynValue? CanTarget;

        internal LuaActionBinding(DynValue callback, DynValue? canTarget)
        {
            Callback = callback;
            CanTarget = canTarget;
        }
    }
}
