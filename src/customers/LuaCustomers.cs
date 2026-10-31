using System;
using System.Collections.Generic;
using MoonSharp.Interpreter;

namespace ProbablyLua.Api
{
    internal sealed class LuaCustomers
    {
        private readonly string _modId;
        private readonly LuaCallbacks _callbacks;
        private readonly Dictionary<string, LuaCustomerDefinition> _definitions = new(StringComparer.Ordinal);
        private bool _sealed;
        private bool _active;

        internal LuaCustomers(string modId, LuaCallbacks callbacks)
        {
            _modId = modId;
            _callbacks = callbacks;
        }

        internal void Bind(Table factory)
        {
            LuaUserData.Register<LuaCustomerDefinition>();
            LuaUserData.Register<LuaCustomerPool>();
            factory.Set("customer", DynValue.NewCallback((_, args) =>
            {
                if (_sealed)
                    throw new ScriptRuntimeException("factory.customer() is only available during Lua initialization.");

                if (args.Count != 1 || args[0].Type != DataType.String)
                    throw new ScriptRuntimeException("factory.customer(id): expected a local customer ID.");

                string id = ResourceIds.NewItem(_modId, args[0].String).FullId;
                if (_definitions.ContainsKey(id))
                    throw new ScriptRuntimeException($"Customer '{id}' is already defined.");

                var customer = new LuaCustomerDefinition(id);
                _definitions.Add(id, customer);
                return UserData.Create(customer);
            }));
        }

        internal void Seal()
        {
            foreach (LuaCustomerDefinition definition in _definitions.Values)
                definition.Validate();

            _sealed = true;
            _active = true;
        }

        internal StoreClient Create(string input)
        {
            if (!_active || _callbacks.IsReadOnly)
                throw new ScriptRuntimeException("Customers can only be added from a gameplay event.");

            string id = ResourceIds.Parse(input, _modId).FullId;
            if (!_definitions.TryGetValue(id, out var definition))
                throw new ScriptRuntimeException($"Customer '{id}' is not defined. Use factory.customer(id) during initialization.");

            definition.Validate();
            var client = new StoreClient
            {
                identifier = id,
                sourceModId = _modId,
                displayName = definition.name,
                clientFaction = definition.faction,
                spriteName = definition.sprite == "random:merc" ? SpriteDict.GetRandomMercSpriteName() : definition.sprite,
                clientIntent = StoreClient.ClientIntent.SELL
            };

            Dialogue line = client.mainDialogue;
            for (int i = 1; i <= definition.dialogue!.Length; i++)
            {
                line.SetText(client.displayName, definition.dialogue.Get(i).String);
                if (i < definition.dialogue.Length)
                    line = line.NextDialogue();
            }

            line.endAction += () =>
            {
                if (_active && definition.on_trade != null)
                    _callbacks.TryInvoke($"Customer '{id}'.on_trade", definition.on_trade);
            };

            client.AddBasicDialog();
            client.CompleteClientCreation();
            return client;
        }

        internal void Clear() => _active = false;
    }
}
