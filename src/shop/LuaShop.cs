using MoonSharp.Interpreter;

namespace ProbablyLua.Api
{
    public sealed class LuaShop
    {
        private readonly LuaItemFactory _items;
        internal LuaShop(LuaItemFactory items) => _items = items;

        public int day
        {
            get { RequireGame(); return StoreStation.Instance.dayCounter; }
        }

        public int black_market_reputation
        {
            get { RequireGame(); return StoreReputation.GetBMReputation(); }
        }

        public bool offer(string itemId, bool stolen = false)
        {
            RequireGame();
            GameItem item = _items.Spawn(itemId);
            bool placed = false;

            try
            {
                placed = PlayerStore.Instance.TryAddDirectSellingItemToTable(
                    item, isStolen: stolen
                );

                return placed;
            }
            finally
            {
                if (!placed && !LuaItemView.IsDestroyed(item) && item.parentInventory == null)
                    LuaItemView.DestroyTree(item);
            }
        }

        private static void RequireGame()
        {
            if (PlayerStore.Instance == null || StoreStation.Instance == null)
                throw new ScriptRuntimeException("shop is only available during gameplay; use it in an event callback.");
        }
    }
}
