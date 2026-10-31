using System;
using System.Collections.Generic;
using System.Globalization;
using MoonSharp.Interpreter;

namespace ProbablyLua.Api
{
    internal static class LogApi
    {
        private const int MaxDepth = 5;
        private const int MaxValues = 128;
        private const int MaxText = 4096;

        private sealed class Budget
        {
            internal int Remaining = MaxValues;
        }

        public static void Register(
            Script script,
            Action<string> info,
            Action<string> warning,
            Action<string> error
        )
        {
            var log = new Table(script);

            log.Set("info", Bind(info));
            log.Set("warning", Bind(warning));
            log.Set("error", Bind(error));

            script.Globals.Set("log", DynValue.NewTable(log));
            script.Globals.Set("print", Bind(info));
        }

        private static DynValue Bind(Action<string> write)
        {
            return DynValue.NewCallback(
                (_, args) =>
                {
                    var values = new string[Math.Min(args.Count, MaxValues)];
                    var budget = new Budget();
                    var visited = new HashSet<Table>();

                    for (int i = 0; i < values.Length; i++)
                        values[i] = Format(args[i], visited, budget);

                    string message = string.Join(" ", values);
                    write(message.Length > MaxText ? message.Substring(0, MaxText) + "..." : message);
                    return DynValue.Nil;
                }
            );
        }

        private static string Format(
            DynValue value,
            HashSet<Table> visited,
            Budget budget,
            int depth = 0,
            bool nested = false
        )
        {
            if (budget.Remaining-- <= 0)
                return "...";

            return value.Type switch
            {
                DataType.String => nested ? Quote(Limit(value.String)) : Limit(value.String),
                DataType.Number => value.Number.ToString(CultureInfo.InvariantCulture),
                DataType.Boolean => value.Boolean ? "true" : "false",
                DataType.Nil or DataType.Void => "nil",
                DataType.Table => FormatTable(value.Table, visited, budget, depth),
                DataType.Function or DataType.ClrFunction => "<function>",
                _ => value.ToPrintString(),
            };
        }

        private static string FormatTable(Table table, HashSet<Table> visited, Budget budget, int depth)
        {
            if (depth >= MaxDepth || budget.Remaining <= 0)
                return "{...}";

            if (!visited.Add(table))
                return "<cycle>";

            try
            {
                var pairs = new List<TablePair>();

                foreach (var pair in table.Pairs)
                {
                    if (pairs.Count >= budget.Remaining)
                        break;
                    if (!pair.Value.IsNil())
                        pairs.Add(pair);
                }

                if (pairs.Count == 0)
                    return "{}";

                if (IsArray(pairs))
                {
                    var values = new string[pairs.Count];

                    foreach (var pair in pairs)
                    {
                        int index = (int)pair.Key.Number - 1;
                        values[index] = Format(pair.Value, visited, budget, depth + 1, true);
                    }

                    return $"[{string.Join(", ", values)}]";
                }

                var entries = new List<string>();

                foreach (var pair in pairs)
                {
                    string key =
                        pair.Key.Type == DataType.String
                            ? Limit(pair.Key.String)
                            : $"[{Format(pair.Key, visited, budget, depth + 1, true)}]";

                    string value = Format(pair.Value, visited, budget, depth + 1, true);
                    entries.Add($"{key} = {value}");
                }

                return $"{{{string.Join(", ", entries)}}}";
            }
            finally
            {
                visited.Remove(table);
            }
        }

        private static bool IsArray(List<TablePair> pairs)
        {
            foreach (var pair in pairs)
            {
                if (pair.Key.Type != DataType.Number)
                    return false;

                double index = pair.Key.Number;

                if (index < 1 || index > pairs.Count || index != Math.Floor(index))
                    return false;
            }

            return true;
        }

        private static string Limit(string value) =>
            value.Length > MaxText ? value.Substring(0, MaxText) + "..." : value;

        private static string Quote(string value)
        {
            return "\""
                + value
                    .Replace("\\", "\\\\")
                    .Replace("\"", "\\\"")
                    .Replace("\n", "\\n")
                    .Replace("\r", "\\r")
                    .Replace("\t", "\\t")
                + "\"";
        }
    }
}
