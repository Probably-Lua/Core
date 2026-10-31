using System;
using System.Collections.Generic;
using System.Diagnostics;
using ProbablyLua.Api;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ProbablyLua
{
    internal sealed class LuaHost
    {
        private static readonly TimeSpan FrameBudget = TimeSpan.FromMilliseconds(4);

        private GameObject? _warningWindow;

        private readonly ModLog _log;
        private readonly string _hostId;

        private readonly Dictionary<string, LuaRuntime> _runtimes = new(StringComparer.OrdinalIgnoreCase);

        private readonly HashSet<string> _seen = new(StringComparer.OrdinalIgnoreCase);

        private readonly Queue<(string Id, string Name, ModLog Log, LuaRuntime Runtime)> _pending = new();

        private readonly Queue<(string Id, string Name)> _warnings = new();
        private readonly Queue<LuaRuntime> _commitOrder = new();
        private readonly LangRegistry _langRegistry = new();
        private readonly ManufacturerRegistry _manufacturers;
        private ModItemDirectory? _directory;

        internal ManufacturerRegistry Manufacturers => _manufacturers;
        internal LangRegistry Languages => _langRegistry;

        public LuaHost(ModLog log, string hostId)
        {
            _log = log;
            _hostId = hostId;
            _manufacturers = new ManufacturerRegistry(_langRegistry);
        }

        public void Load(LoadedMod mod)
        {
            string id = mod.Manifest.ID;

            if (!mod.IsEnabled || string.IsNullOrWhiteSpace(id) ||
                string.Equals(id, _hostId, StringComparison.OrdinalIgnoreCase) ||
                _seen.Contains(id))
                return;

            string name = string.IsNullOrWhiteSpace(mod.Manifest.Name)
                ? id
                : mod.Manifest.Name;

            var modLog = new ModLog(mod.Manifest);

            try
            {
                if (!ModHelper.ModFileExists(id, LuaScriptPaths.Entry))
                    return;

                _seen.Add(id);

                string source = LuaFileLoader.ReadSource(id, LuaScriptPaths.Entry);

                if (string.IsNullOrWhiteSpace(source))
                {
                    modLog.Warning($"Empty Lua entry point: {LuaScriptPaths.Entry}");
                    return;
                }

                var runtime = new LuaRuntime(
                    id,
                    name,
                    _langRegistry,
                    modLog.Log,
                    modLog.Warning,
                    modLog.Error
                );

                runtime.BeginLoad(source);

                _pending.Enqueue((id, name, modLog, runtime));
                _commitOrder.Enqueue(runtime);
            }
            catch (TimeoutException exception)
            {
                ReportTimeout(id, name, modLog, exception);
            }
            catch (Exception exception)
            {
                modLog.Error($"Lua initialization failed: {LuaErrorFormatter.Describe(exception)}");
            }
        }

        public void Tick()
        {
            if (_pending.Count == 0 && _commitOrder.Count == 0 && _warnings.Count == 0)
                return;

            var frame = Stopwatch.StartNew();
            int count = _pending.Count;

            for (int i = 0; i < count && frame.Elapsed < FrameBudget; i++)
            {
                var (id, name, modLog, runtime) = _pending.Dequeue();

                try
                {
                    if (runtime.Step())
                    {
                        runtime.MarkSucceeded();
                        modLog.Log("Lua initialization completed.");
                    }
                    else
                    {
                        _pending.Enqueue((id, name, modLog, runtime));
                    }
                }
                catch (TimeoutException exception)
                {
                    runtime.MarkFailed();
                    ReportTimeout(id, name, modLog, exception);
                }
                catch (Exception exception)
                {
                    runtime.MarkFailed();
                    modLog.Error($"Lua initialization failed: {LuaErrorFormatter.Describe(exception)}");
                }
            }

            CommitCompleted();
            ShowWarning();
        }

        private void CommitCompleted()
        {
            // Completion can happen out of order because Lua init runs in slices.
            // Apply changes in the game's original mod load order.
            while (_commitOrder.Count > 0 && _commitOrder.Peek().IsFinished)
            {
                LuaRuntime runtime = _commitOrder.Dequeue();
                if (!runtime.Succeeded)
                    continue;

                try
                {
                    runtime.CommitTranslations();
                    _manufacturers.Register(runtime.Manufacturers);
                    runtime.CommitContextActions();
                    if (_directory != null)
                        runtime.RegisterItems(_directory);

                    _runtimes.Add(runtime.ModId, runtime);
                }
                catch (Exception exception)
                {
                    runtime.MarkFailed();
                    _log.Error(LuaErrorFormatter.Format($"Committing mod '{runtime.ModId}'", exception));
                }
            }
        }

        private void ReportTimeout(
            string id,
            string name,
            ModLog modLog,
            TimeoutException exception
        )
        {
            modLog.Error($"Lua initialization stopped: {exception.Message}");
            _warnings.Enqueue((id, name));
        }

        private void ShowWarning()
        {
            if (_warnings.Count == 0)
                return;

            if (_warningWindow != null || EventSystem.current == null)
                return;

            var (id, name) = _warnings.Peek();

            try
            {
                _warningWindow = LuaWarningWindow.Show(id, name);
                _warnings.Dequeue();
            }
            catch (Exception exception)
            {
                _warnings.Dequeue();
                _log.Error($"Failed to display Lua warning: {exception}");
            }
        }

        public void Emit(string eventName, params object[] args)
        {
            FinishPending();
            foreach (LuaRuntime runtime in _runtimes.Values)
                runtime.Emit(eventName, args);
        }

        public void RegisterItems(ModItemDirectory directory)
        {
            // Definitions must exist before SaveManager resolves saved IDs.
            // Normally Update has finished them in the main menu. If the player
            // starts immediately, finish the bounded jobs at this load barrier.
            FinishPending();

            _directory = directory;
            foreach (var pair in _runtimes)
            {
                try
                {
                    pair.Value.RegisterItems(directory);
                }
                catch (Exception exception)
                {
                    pair.Value.MarkFailed();
                    _log.Error(LuaErrorFormatter.Format($"Registering items for mod '{pair.Key}'", exception));
                }
            }

        }

        internal void GenerateCustomers(StoreClientManager manager)
        {
            FinishPending();
            foreach (LuaRuntime runtime in _runtimes.Values)
                runtime.GenerateCustomers(manager);
        }

        private void FinishPending()
        {
            while (_pending.Count > 0)
                Tick();
        }

        public void Clear()
        {
            foreach (LuaRuntime runtime in _runtimes.Values)
                runtime.ClearContextActions();

            foreach (LuaRuntime runtime in _commitOrder)
                runtime.ClearContextActions();

            _pending.Clear();
            _runtimes.Clear();
            _seen.Clear();
            _warnings.Clear();
            _commitOrder.Clear();
            _manufacturers.Clear();
            _langRegistry.Clear();
            _directory = null;

            if (_warningWindow != null)
                UnityEngine.Object.Destroy(_warningWindow);

            _warningWindow = null;
        }
    }
}
