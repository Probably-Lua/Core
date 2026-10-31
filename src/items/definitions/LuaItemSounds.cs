

namespace ProbablyLua.Api
{
    // Optional sound slots mirror GameItem. Nil keeps native defaults; an empty string silences the slot.
    // Bare IDs refer to this mod; base:<id> refers to a built-in game sound.
    public sealed class LuaItemSounds
    {
        public string? drag_start { get; set; }
        public string? drag_end { get; set; }
        public string? container_open { get; set; }
        public string? container_close { get; set; }
        public string? select { get; set; }
        public string? examine_open { get; set; }
        public string? toggle { get; set; }
        public string? activate { get; set; }
        public string? use { get; set; }
    }
}
