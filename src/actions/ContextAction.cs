using System;
using System.Collections.Generic;
using ProbablyLua.Api;

namespace ProbablyLua
{
    // An action belongs to a particular item definition. A new registration
    // with the same item/action IDs replaces the previous handler.
    public sealed class ContextAction
    {
        public string ItemId { get; set; } = "";
        public string Id { get; set; } = "";
        public Func<GameItem, string>? Label { get; set; }
        public Action<GameItem>? Execute { get; set; }
        public Func<GameItem, bool>? CanExecute { get; set; }
        public bool IsSuppressed { get; set; }
    }
}
