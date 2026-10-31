using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace ProbablyLua.Api
{
    // One synchronous UI pass may ask MayTarget/CanTarget for the same pair
    // several times. Reuse only those inspection results, never click results
    // or results from an earlier frame/input event.
    internal sealed class LuaTargetProbeScope : IDisposable
    {
        private const int MaxEntries = 4096;
        [ThreadStatic] private static LuaTargetProbeScope? _current;
        private readonly LuaTargetProbeScope? _previous;
        private readonly GameItem? _startingDrag;
        private readonly Dictionary<Key, bool> _results = new();
        private bool _disposed;

        private LuaTargetProbeScope(GameItem? startingDrag)
        {
            _previous = _current;
            _startingDrag = startingDrag;
            _current = this;
        }

        internal static LuaTargetProbeScope? Begin(GameItem? source, bool startingDrag = false) =>
            source != null && LuaItemLifecycle.IsManaged(source)
                ? new LuaTargetProbeScope(startingDrag ? source : null)
                : null;

        internal static LuaTargetProbeScope? BeginGroup(IEnumerable<GameItemElement> sources)
        {
            foreach (GameItem source in sources)
                if (LuaItemLifecycle.IsManaged(source))
                    return new LuaTargetProbeScope(null);

            return null;
        }

        internal static bool IsStartingDrag(GameItem source)
        {
            for (LuaTargetProbeScope? scope = _current; scope != null; scope = scope._previous)
                if (ReferenceEquals(scope._startingDrag, source))
                    return true;

            return false;
        }

        internal static bool TryGet(GameItem source, GameItem target, LuaActionBinding binding, out bool result)
        {
            result = false;
            return _current != null && _current._results.TryGetValue(new Key(source, target, binding), out result);
        }

        internal static void Store(GameItem source, GameItem target, LuaActionBinding binding, bool result)
        {
            if (_current != null && _current._results.Count < MaxEntries)
                _current._results[new Key(source, target, binding)] = result;
        }

        internal static void Invalidate()
        {
            for (LuaTargetProbeScope? scope = _current; scope != null; scope = scope._previous)
                scope._results.Clear();
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            _results.Clear();
            _current = _previous;
        }

        private readonly struct Key : IEquatable<Key>
        {
            private readonly GameItem _source;
            private readonly GameItem _target;
            private readonly LuaActionBinding _binding;

            internal Key(GameItem source, GameItem target, LuaActionBinding binding)
            {
                _source = source;
                _target = target;
                _binding = binding;
            }

            public bool Equals(Key other) => ReferenceEquals(_source, other._source) &&
                ReferenceEquals(_target, other._target) && ReferenceEquals(_binding, other._binding);

            public override bool Equals(object? other) => other is Key key && Equals(key);

            public override int GetHashCode()
            {
                unchecked
                {
                    return (RuntimeHelpers.GetHashCode(_source) * 397 ^ RuntimeHelpers.GetHashCode(_target)) * 397 ^
                        RuntimeHelpers.GetHashCode(_binding);
                }
            }
        }
    }
}
