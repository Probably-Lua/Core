using System;
using System.Collections.Generic;
using MoonSharp.Interpreter;

namespace ProbablyLua.Api
{
    internal enum LuaActionKind
    {
        Immediate,
        Target,
        Equip
    }
}
