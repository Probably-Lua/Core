using MoonSharp.Interpreter;

namespace ProbablyLua.Api
{
    // This table contains actual game item types, not every possible item tag.
    // Custom type names can be passed as ordinary strings.
    internal static class ItemTypeApi
    {
        public static string Require(string value, string field = "Item type") => LuaValues.Text(value, field);

        public static void Bind(Script script)
        {
            var types = new Table(script);
            foreach (string type in TypeHelper.ALL_TYPES_LIST)
                types.Set(type, DynValue.NewString(type));

            script.Globals.Set("ItemType", DynValue.NewTable(types));
        }
    }
}
