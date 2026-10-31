using ProbablyLua.Api;
using UnityEngine;

namespace ProbablyLua
{
    public sealed class ProbablyLuaMod : IMod
    {
        private LuaHost? _host;
        private LuaUpdateDriver? _driver;

        public void Init(ModManifest manifest)
        {
            _host = new LuaHost(new ModLog(manifest), manifest.ID);
            ProbablyLuaSettings.Initialize(manifest.ID);
        }

        public void OnEnable()
        {
            // ModLoader applies this assembly's Harmony patches before Init().
            ManufacturerDisplayPatch.Registry = _host?.Manufacturers;
            TypeLocalizationPatch.Registry = _host?.Languages;
            LuaCustomerTradePatch.Host = _host;
            var obj = new GameObject("ProbablyLua Driver");

            Object.DontDestroyOnLoad(obj);

            _driver = obj.AddComponent<LuaUpdateDriver>();
            _driver.Host = _host;

            ModHook.OnModAssetsLoaded += OnAssetsLoaded;
            ModHook.OnGameLoadedNormal += OnGameLoaded;
            ModHook.OnNewGameStarted += OnNewGameStarted;
            ModHook.OnModItemDirectoryInit += OnItemDirectoryInit;
            ModHook.OnGenerateCustomerNormal += OnGenerateCustomers;
            ModHook.OnLootTablesLoaded += OnLootReady;
        }

        public void OnDisable()
        {
            ContextMenuController.RestoreCurrent();
            ManufacturerDisplayPatch.Registry = null;
            TypeLocalizationPatch.Registry = null;
            LuaCustomerTradePatch.Host = null;
            ModHook.OnModAssetsLoaded -= OnAssetsLoaded;
            ModHook.OnGameLoadedNormal -= OnGameLoaded;
            ModHook.OnNewGameStarted -= OnNewGameStarted;
            ModHook.OnModItemDirectoryInit -= OnItemDirectoryInit;
            ModHook.OnGenerateCustomerNormal -= OnGenerateCustomers;
            ModHook.OnLootTablesLoaded -= OnLootReady;

            _host?.Clear();

            if (_driver != null)
                Object.Destroy(_driver.gameObject);

            _driver = null;
        }

        private void OnAssetsLoaded(LoadedMod mod) => _host?.Load(mod);

        private void OnGameLoaded() => _host?.Emit("game_loaded");

        private void OnNewGameStarted(string scenarioId) => _host?.Emit("new_game_started", scenarioId);

        private void OnItemDirectoryInit(ModItemDirectory directory) => _host?.RegisterItems(directory);

        private void OnLootReady() => _host?.Emit("loot_ready");

        private void OnGenerateCustomers(StoreClientManager manager) => _host?.GenerateCustomers(manager);
    }
}
