using MoonSharp.Interpreter;

namespace ProbablyLua.Api
{
    internal static class ContrabandLevelApi
    {
        private const string Prefix = "CONTRABAND_LEVEL_";

        internal static void Bind(Script script)
        {
            var values = new Table(script);
            values.Set("None", DynValue.NewString(Prefix + "NONE"));
            values.Set("Low", DynValue.NewString(Prefix + "LOW"));
            values.Set("Mid", DynValue.NewString(Prefix + "MID"));
            values.Set("High", DynValue.NewString(Prefix + "HIGH"));
            values.Set("Critical", DynValue.NewString(Prefix + "CRITICAL"));
            script.Globals.Set("ContrabandLevel", DynValue.NewTable(values));
        }

        internal static ContrabandLevelKind Require(string value)
        {
            switch (value)
            {
                case Prefix + "NONE": return ContrabandLevelKind.None;
                case Prefix + "LOW": return ContrabandLevelKind.Low;
                case Prefix + "MID": return ContrabandLevelKind.Mid;
                case Prefix + "HIGH": return ContrabandLevelKind.High;
                case Prefix + "CRITICAL": return ContrabandLevelKind.Critical;
                default:
                    throw new ScriptRuntimeException($"Invalid contraband level '{value ?? "nil"}'. Use ContrabandLevel.None, Low, Mid, High or Critical.");
            }
        }

        internal static string ToGameValue(ContrabandLevelKind level) => Prefix + level.ToString().ToUpperInvariant();

        internal static void Apply(GameItem item, ContrabandLevelKind level)
        {
            if (level == ContrabandLevelKind.None)
            {
                ContrabandHelper.RemoveContrabandStatus(item);
                return;
            }

            ContrabandHelper.InitContrabandItem(item, (int)level);
        }
    }
}
