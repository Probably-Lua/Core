using System;
using System.Collections.Generic;
using System.IO;
using MoonSharp.Interpreter;

namespace ProbablyLua.Api
{
    // Sandboxed dofile: load sources through ModHelper for the current mod,
    // never through System.IO or a shared Lua filesystem.
    internal sealed class LuaFileLoader
    {
        internal const int MaxFileLength = 512 * 1024;
        private const int MaxFileCalls = 256;
        private readonly Script _script;
        private readonly string _modId;
        private int _calls;
        private bool _sealed;

        internal LuaFileLoader(Script script, string modId)
        {
            _script = script;
            _modId = modId;
        }

        internal void Bind() => _script.Globals.Set("dofile", DynValue.NewCallback(DoFile));

        internal void Seal() => _sealed = true;

        internal static string ReadSource(string modId, string path)
        {
            // ModHelper confines paths lexically. Also reject links below the
            // mod root, so a linked lua/ file cannot read another directory.
            string current = ModHelper.GetModFolder(modId);
            foreach (string segment in path.Split('/'))
            {
                current = Path.Combine(current, segment);
                if ((File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0)
                    throw new ScriptRuntimeException($"Lua source '{path}' uses a symbolic link. Only regular files inside this mod's lua/ directory are allowed.");
            }

            if (new FileInfo(current).Length > MaxFileLength)
                throw new ScriptRuntimeException($"Lua source '{path}' exceeds the 512 KiB source limit.");

            string? source = ModHelper.ReadModFileText(modId, path);
            if (source == null)
                throw new ScriptRuntimeException($"Could not read Lua source '{path}' in mod '{modId}'.");

            if (source.Length > MaxFileLength)
                throw new ScriptRuntimeException($"Lua source '{path}' exceeds the 512 KiB source limit.");

            return source;
        }

        private DynValue DoFile(ScriptExecutionContext _, CallbackArguments args)
        {
            if (_sealed)
                throw new ScriptRuntimeException("dofile() is only available during Lua initialization. Load modules from lua/init.lua and keep their returned functions.");

            if (args.Count != 1 || args[0].Type != DataType.String)
                throw new ScriptRuntimeException("dofile(path) expects a relative Lua file path.");

            if (++_calls > MaxFileCalls)
                throw new ScriptRuntimeException($"dofile() limit exceeded: at most {MaxFileCalls} Lua files per mod initialization. Check for recursive includes.");

            string path = Resolve(args[0].String);
            if (!ModHelper.ModFileExists(_modId, path))
                throw new ScriptRuntimeException($"dofile('{args[0].String}'): file '{path}' was not found in mod '{_modId}'. Paths are relative to lua/.");

            string source = ReadSource(_modId, path);

            DynValue chunk = _script.LoadString(source, codeFriendlyName: path);
            // The included chunk runs in the caller's coroutine: unlike
            // Script.Call(chunk), this preserves AutoYieldCounter and the
            // existing Lua initialization time-slicing limit.
            return DynValue.NewTailCallReq(chunk);
        }

        private static string Resolve(string input)
        {
            if (string.IsNullOrWhiteSpace(input) || input.IndexOf('\0') >= 0 ||
                input.IndexOf(':') >= 0 || input.StartsWith("/", StringComparison.Ordinal))
                throw new ScriptRuntimeException("dofile() requires a relative path inside lua/.");

            string normalized = input.Replace('\\', '/');
            var parts = new List<string> { LuaScriptPaths.Root };
            if (normalized.StartsWith(LuaScriptPaths.Root + "/", StringComparison.Ordinal))
                normalized = normalized.Substring(LuaScriptPaths.Root.Length + 1);

            foreach (string part in normalized.Split('/'))
            {
                if (part.Length == 0 || part == ".")
                    continue;

                if (part == "..")
                {
                    if (parts.Count <= 1)
                        throw new ScriptRuntimeException("dofile() cannot escape lua/.");

                    parts.RemoveAt(parts.Count - 1);
                }
                else
                {
                    parts.Add(part);
                }
            }

            if (parts.Count < 2)
                throw new ScriptRuntimeException("dofile() requires a Lua file inside lua/.");

            if (!parts[parts.Count - 1].EndsWith(".lua", StringComparison.Ordinal))
                throw new ScriptRuntimeException("dofile() only accepts .lua source files.");

            return string.Join("/", parts);
        }
    }
}
