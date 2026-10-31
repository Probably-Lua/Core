using System;
using System.Collections.Generic;
using System.Diagnostics;
using MoonSharp.Interpreter;
using ProbablyLua.Api;

namespace ProbablyLua
{
    internal sealed partial class LuaRuntime
    {
        private readonly Script _script;
        private readonly EventsApi _events;
        private readonly LuaItemFactory _items;
        private readonly LangApi _lang;
        private readonly LuaManufacturerFactory _manufacturers;
        private readonly LuaItemActions _itemActions;
        private readonly LuaActionCatalog _actionTypes;
        private readonly LuaFileLoader _fileLoader;
        private readonly LuaCustomers _customers;
        private readonly LuaLootApi _loot;
        internal string ModId { get; }
        private bool _ready;
        private bool _failed;

        internal bool IsFinished => _ready || _failed;
        internal bool Succeeded => _ready;
        internal IEnumerable<LuaManufacturer> Manufacturers => _manufacturers.Definitions;

        public LuaRuntime(
            string modId,
            string modName,
            LangRegistry registry,
            Action<string> info,
            Action<string> warning,
            Action<string> error
        )
        {
            ModId = modId;
            _script = LuaSandbox.Create();
            _script.Options.DebugPrint = info;
            _fileLoader = new LuaFileLoader(_script, modId);
            _fileLoader.Bind();

            LogApi.Register(_script, info, warning, error);

            var callbacks = new LuaCallbacks(_script, error);
            _loot = new LuaLootApi(modId, _script, callbacks);
            _loot.Bind();
            _events = new EventsApi(_script, callbacks);
            _events.Register();

            ManufacturerColors.Bind(_script);
            ItemTypeApi.Bind(_script);
            ContrabandLevelApi.Bind(_script);
            LuaSoundPresets.Bind(_script);

            _lang = new LangApi(modId, registry, callbacks);
            _lang.Bind(_script);

            var factory = new Table(_script);
            _manufacturers = new LuaManufacturerFactory(modId);
            _manufacturers.Bind(factory);
            _actionTypes = new LuaActionCatalog(modId);
            _actionTypes.Bind(factory, _script);
            _items = new LuaItemFactory(modId, modName, _lang, _actionTypes, callbacks, warning);
            _items.Bind(factory, _script);
            _itemActions = new LuaItemActions(_script, _items, _lang, _actionTypes, callbacks, error);
            _itemActions.Bind(_script);
            _items.OnItemCreated = _itemActions.AttachItem;
            _customers = new LuaCustomers(modId, callbacks);
            _customers.Bind(factory);
            LuaUserData.Register<LuaShop>();
            _script.Globals.Set("shop", UserData.Create(new LuaShop(_items)));
            _script.Globals.Set("factory", DynValue.NewTable(factory));
        }

        internal void MarkSucceeded()
        {
            _fileLoader.Seal();
            _items.Seal();
            _manufacturers.Seal();
            _customers.Seal();
            _ready = true;
        }

        internal void MarkFailed()
        {
            _ready = false;
            _failed = true;
            ClearContextActions();
        }

        internal void CommitTranslations() => _lang.Commit();

        internal void CommitContextActions()
        {
            _actionTypes.Seal();
            _itemActions.Commit();
            _loot.Commit();
        }

        internal void ClearContextActions()
        {
            _ready = false;
            _itemActions.Clear();
            _items.Clear();
            _customers.Clear();
            _loot.Clear();
        }

        internal void GenerateCustomers(StoreClientManager manager)
        {
            if (!_ready)
                return;

            var pool = new LuaCustomerPool(manager, _customers);
            try { _events.Emit("generate_customers", pool); }
            finally { pool.Close(); }
        }

        public void RegisterItems(ModItemDirectory directory)
        {
            if (_ready)
                _items.RegisterItems(directory);
        }

        public void Emit(string eventName, params object[] args)
        {
            if (_ready)
                _events.Emit(eventName, args);
        }
    }
}
