using System;
using System.Collections.Generic;
using MoonSharp.Interpreter;

namespace ProbablyLua.Api
{
    public sealed class LuaCustomerPool
    {
        private StoreClientManager? _manager;
        private readonly LuaCustomers _customers;
        private int _count;

        internal LuaCustomerPool(StoreClientManager manager, LuaCustomers customers)
        {
            _manager = manager;
            _customers = customers;
        }

        public void add(string id)
        {
            if (_manager == null)
                throw new ScriptRuntimeException("The customer-generation event has ended. Do not keep its pool for later use.");

            if (++_count > 32)
                throw new ScriptRuntimeException("A mod may add at most 32 customers per generation event.");

            _manager.AddClient(_customers.Create(id));
        }

        internal void Close() => _manager = null;
    }
}
