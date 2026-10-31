using System;
using System.Runtime.CompilerServices;
using HarmonyLib;

namespace ProbablyLua.Api
{
    // DirectoryMaster removes constructor-created children. Initialization must
    // run AFTER that cleanup, regardless of which gameplay system requested it.
    internal static class LuaItemLifecycle
    {
        private sealed class Entry
        {
            internal readonly LuaItemFactory Factory;
            internal readonly LuaItemDefinition Definition;
            internal bool Initialized;

            internal Entry(LuaItemFactory factory, LuaItemDefinition definition)
            {
                Factory = factory;
                Definition = definition;
            }
        }

        private static readonly ConditionalWeakTable<GameItem, Entry> Entries = new();
        [ThreadStatic] private static int _suppressed;

        internal static void Track(GameItem item, LuaItemFactory factory, LuaItemDefinition definition) =>
            Entries.Add(item, new Entry(factory, definition));

        internal static bool IsManaged(GameItem item) => Entries.TryGetValue(item, out _);

        internal static void Created(GameItem item)
        {
            if (item == null || !Entries.TryGetValue(item, out Entry entry) || entry.Initialized)
                return;

            entry.Initialized = true;
            if (SaveManager.IsDecoding || _suppressed != 0)
                return;

            entry.Factory.Initialize(entry.Definition, item);
        }

        internal static void Unlocked(GameItem item)
        {
            if (Entries.TryGetValue(item, out Entry entry))
                entry.Factory.QueueNotification("on_unlocked", entry.Definition, item, entry.Definition.on_unlocked);
        }

        internal static IDisposable Suppress() => new Suppression();

        private sealed class Suppression : IDisposable
        {
            private bool _disposed;
            internal Suppression() => _suppressed++;
            public void Dispose()
            {
                if (_disposed)
                    return;

                _disposed = true;
                _suppressed--;
            }
        }
    }
}
