using MoonSharp.Interpreter;

namespace ProbablyLua.Api
{
    // The game stores contraband severity as a string value in its
    // CONTRABAND_LEVEL state, not as a GameItemType (RESTRICTED_1, etc.).
    internal enum ContrabandLevelKind
    {
        None,
        Low,
        Mid,
        High,
        Critical
    }
}
