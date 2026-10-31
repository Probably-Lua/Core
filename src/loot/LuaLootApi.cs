using System;
using System.Collections.Generic;
using MoonSharp.Interpreter;

namespace ProbablyLua.Api
{
    // Lua sees strings, numbers and copied tables only. Initialization edits
    // are staged so a failing mod cannot leave half of its loot changes behind.
    internal sealed class LuaLootApi
    {
        private const int MaxOperations = 4096;
        private const int MaxEntries = 1024;
        private readonly string _modId;
        private readonly Script _script;
        private readonly LuaCallbacks _callbacks;
        private readonly List<Action> _pending = new();
        private readonly HashSet<string> _declared = new(StringComparer.Ordinal);
        private int _operations;
        private bool _committed;
        private bool _disabled;

        internal LuaLootApi(string modId, Script script, LuaCallbacks callbacks)
        {
            _modId = modId;
            _script = script;
            _callbacks = callbacks;
        }

        internal void Bind()
        {
            Table api = MakeApi(group: false);
            api.Set("groups", DynValue.NewTable(MakeApi(group: true)));
            _script.Globals.Set("loot", DynValue.NewTable(api));
        }

        private Table MakeApi(bool group)
        {
            var api = new Table(_script);
            foreach (string name in new[] { "register", "update", "replace", "set_weight", "remove", "clear", "entries", "roll", "ids" })
            {
                string operation = name;
                api.Set(name, DynValue.NewCallback((_, args) => Dispatch(group, operation, args)));
            }
            return api;
        }

        private DynValue Dispatch(bool group, string operation, CallbackArguments args)
        {
            string method = (group ? "loot.groups." : "loot.") + operation;
            bool read = operation == "entries" || operation == "ids" || operation == "roll";
            int count = operation == "ids" ? 0 :
                operation == "entries" || operation == "roll" || operation == "clear" ? 1 :
                operation == "set_weight" ? 3 : 2;

            if (args.Count != count || (count > 0 && args[0].Type != DataType.String))
                throw new ScriptRuntimeException($"{method}(): expected {count} argument(s); the first argument is a table/group ID string.");

            if (_disabled)
                throw new ScriptRuntimeException($"{method}(): this Lua mod is disabled.");

            if (read && !_committed)
                throw new ScriptRuntimeException($"{method}(): wait for the loot_ready or game_loaded event before reading or rolling loot.");

            if ((!read || operation == "roll") && _callbacks.IsReadOnly)
                throw new ScriptRuntimeException($"{method}(): cannot change or roll loot from a can_target filter.");

            if (operation == "ids")
            {
                var ids = new List<string>(group ? LootRegistry.GroupIds : LootRegistry.TableIds);
                ids.Sort(StringComparer.Ordinal);
                var result = new Table(_script);
                foreach (string id in ids)
                    result.Append(DynValue.NewString(Qualify(id)));

                return DynValue.NewTable(result);
            }

            string target = NativeId(args[0].String);
            if (read || (_committed && operation != "register"))
                RequireExists(group, target, method);

            if (operation == "entries")
            {
                var result = new Table(_script);
                List<LootEntry> entries = group ? ModHelper.GetLootGroupEntries(target) : ModHelper.GetLootEntries(target);
                foreach (LootEntry entry in entries)
                    result.Set(Qualify(entry.id), DynValue.NewNumber(entry.weight));

                return DynValue.NewTable(result);
            }

            if (operation == "roll")
            {
                string? itemId = group ? ModHelper.RollLootGroup(target) : ModHelper.RollLootTable(target);
                return string.IsNullOrEmpty(itemId) ? DynValue.Nil : DynValue.NewString(Qualify(itemId));
            }

            if (operation == "clear")
            {
                Queue(new[] { ClearOperation(group, target) });
                return DynValue.Nil;
            }

            if (operation == "remove" || operation == "set_weight")
            {
                if (args[1].Type != DataType.String)
                    throw new ScriptRuntimeException($"{method}(): the entry ID must be a string.");

                string entry = NativeId(args[1].String);
                Action change;
                if (operation == "remove")
                    change = group ? () => ModHelper.RemoveLootGroupEntry(_modId, target, entry)
                        : () => ModHelper.RemoveLootEntry(_modId, target, entry);
                else
                {
                    float weight = Weight(args[2], method, allowZero: true);
                    change = group ? () => ModHelper.SetLootGroupWeight(_modId, target, entry, weight)
                        : () => ModHelper.SetLootWeight(_modId, target, entry, weight);
                }
                Queue(new[] { change });
                return DynValue.Nil;
            }

            if (args[1].Type != DataType.Table)
                throw new ScriptRuntimeException($"{method}(): expected an ID-to-weight table, e.g. {{ ['base:screwdriver'] = 10, soap = 30 }}.");

            var weights = ReadWeights(args[1].Table, method);
            var changes = new List<Action>();
            if (operation == "register")
            {
                ResourceId id = ResourceIds.Parse(args[0].String, _modId);
                if (id.ModId != _modId)
                    throw new ScriptRuntimeException($"{method}(): new loot tables and groups must belong to this mod. Use update or replace to edit an existing table.");

                if (weights.Count == 0)
                    throw new ScriptRuntimeException($"{method}(): a new table or group needs at least one positive-weight entry.");

                string key = (group ? "group:" : "table:") + target;
                if (_declared.Contains(key) || (group ? LootRegistry.HasGroup(target) : LootRegistry.HasTable(target)))
                    throw new ScriptRuntimeException($"{method}(): '{args[0].String}' is already registered; use update or replace.");

                var entries = weights.ToArray();
                changes.Add(group ? () => ModHelper.RegisterLootGroup(_modId, target, entries)
                    : () => ModHelper.RegisterLootTable(_modId, target, entries));

                Queue(changes);
                _declared.Add(key);
                return DynValue.Nil;
            }

            if (operation == "replace")
                changes.Add(ClearOperation(group, target));

            foreach (var (entry, weight) in weights)
            {
                string capturedId = entry;
                float capturedWeight = weight;
                changes.Add(group ? () => ModHelper.AddLootGroupEntry(_modId, target, capturedId, capturedWeight)
                    : () => ModHelper.AddLootEntry(_modId, target, capturedId, capturedWeight));
            }
            Queue(changes);
            return DynValue.Nil;
        }

        private List<(string Id, float Weight)> ReadWeights(Table table, string method)
        {
            var entries = new List<(string Id, float Weight)>();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (TablePair pair in table.Pairs)
            {
                if (entries.Count >= MaxEntries)
                    throw new ScriptRuntimeException($"{method}(): at most {MaxEntries} entries are allowed per call.");

                if (pair.Key.Type != DataType.String)
                    throw new ScriptRuntimeException($"{method}(): entry IDs must be strings.");

                string id = NativeId(pair.Key.String);
                if (!seen.Add(id))
                    throw new ScriptRuntimeException($"{method}(): duplicate entry '{id}' after resolving its namespace.");

                entries.Add((id, Weight(pair.Value, method, allowZero: false)));
            }
            entries.Sort((a, b) => StringComparer.Ordinal.Compare(a.Id, b.Id));
            return entries;
        }

        private static float Weight(DynValue value, string method, bool allowZero)
        {
            if (value.Type != DataType.Number || double.IsNaN(value.Number) || double.IsInfinity(value.Number) ||
                value.Number > 1000000000 || (allowZero ? value.Number < 0 : value.Number <= 0) ||
                (!allowZero && (float)value.Number == 0))
                throw new ScriptRuntimeException($"{method}(): weight must be finite, {(allowZero ? "nonnegative" : "positive")} " +
                    "and at most 1000000000. Weights are relative, not percentages.");

            return (float)value.Number;
        }

        private Action ClearOperation(bool group, string id) => group
            ? () => ModHelper.ClearLootGroup(_modId, id)
            : () => ModHelper.ClearLootTable(_modId, id);

        private void Queue(IEnumerable<Action> operations)
        {
            var batch = new List<Action>(operations);
            if (_operations + batch.Count > MaxOperations)
                throw new ScriptRuntimeException($"Loot edit limit exceeded: at most {MaxOperations} journal operations per mod per session. Avoid rewriting tables every frame.");

            _operations += batch.Count;
            if (_committed)
            {
                foreach (Action apply in batch)
                    apply();
            }
            else
                _pending.AddRange(batch);
        }

        private string NativeId(string input)
        {
            ResourceId id = ResourceIds.Parse(input, _modId);
            return id.IsBase ? id.LocalId : id.FullId;
        }

        private static string Qualify(string id) => id.Contains(":") ? id : "base:" + id;

        private static void RequireExists(bool group, string id, string method)
        {
            if (!(group ? LootRegistry.HasGroup(id) : LootRegistry.HasTable(id)))
                throw new ScriptRuntimeException($"{method}(): '{Qualify(id)}' does not exist. Use ids() to inspect loaded IDs, or register a local table first.");
        }

        internal void Commit()
        {
            if (_committed)
                return;

            foreach (Action apply in _pending)
                apply();

            _pending.Clear();
            _committed = true;
        }

        internal void Clear()
        {
            _pending.Clear();
            _disabled = true;
        }
    }
}
