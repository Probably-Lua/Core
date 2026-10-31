using System;
using System.Collections.Generic;
using ProbablyLua.Api;

namespace ProbablyLua
{
    public static class ContextActions
    {
        private static readonly Dictionary<string, Dictionary<string, ContextAction>> Actions =
            new(StringComparer.OrdinalIgnoreCase);

        internal static bool HasAnyActions => Actions.Count != 0;

        public static void Register(ContextAction action)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            string itemId = ResourceIds.RequireQualified(action.ItemId);
            string actionId = ResourceIds.RequireActionId(action.Id);
            if (!action.IsSuppressed && action.Execute == null)
                throw new ArgumentException($"Context action '{action.Id}' for item '{action.ItemId}' needs an Execute callback unless IsSuppressed is true.", nameof(action));

            if (action.IsSuppressed && !ResourceIds.IsVanillaAction(actionId))
                throw new ArgumentException($"Context action '{action.Id}' cannot be suppressed: only built-in actions can be suppressed. " +
                    "Unregister custom actions instead.", nameof(action));

            action.ItemId = itemId;
            action.Id = actionId;
            if (!Actions.TryGetValue(itemId, out var entries))
            {
                entries = new Dictionary<string, ContextAction>(StringComparer.OrdinalIgnoreCase);
                Actions.Add(itemId, entries);
            }

            // Exactly one handler per item/action ID; the latest registration wins.
            entries[actionId] = action;
        }

        public static bool Unregister(string itemId, string actionId)
        {
            itemId = ResourceIds.RequireQualified(itemId);
            actionId = ResourceIds.RequireActionId(actionId);
            if (!Actions.TryGetValue(itemId, out var entries) || !entries.Remove(actionId))
                return false;

            if (entries.Count == 0)
                Actions.Remove(itemId);

            return true;
        }

        // Do not remove a registration that another C# mod has replaced.
        internal static bool UnregisterIfSame(ContextAction action)
        {
            if (!IsRegistered(action))
                return false;

            return Unregister(action.ItemId, action.Id);
        }

        internal static bool IsRegistered(ContextAction action) =>
            Actions.TryGetValue(action.ItemId, out var entries) &&
            entries.TryGetValue(action.Id, out var current) &&
            ReferenceEquals(current, action);

        internal static List<ContextAction> SnapshotFor(string itemId)
        {
            var result = new List<ContextAction>();
            if (Actions.TryGetValue(itemId, out var entries))
                result.AddRange(entries.Values);

            result.Sort((a, b) => StringComparer.OrdinalIgnoreCase.Compare(a.Id, b.Id));
            return result;
        }
    }
}
