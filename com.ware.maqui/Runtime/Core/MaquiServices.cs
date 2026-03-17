using System;
using System.Collections.Generic;

namespace Maqui.Core
{
    /// <summary>
    /// Lightweight static service locator for Maqui core services.
    /// CoreBootstrap registers all services at boot. Tests call Reset() for clean isolation.
    /// Mirrors HybridFrame's HF.Get/HF.Register pattern for ORO integration.
    /// </summary>
    public static class MaquiServices
    {
        private static readonly Dictionary<Type, object> _services = new();

        public static void Register<T>(T instance) where T : class
        {
            _services[typeof(T)] = instance;
        }

        public static T Get<T>() where T : class
        {
            _services.TryGetValue(typeof(T), out var service);
            return service as T;
        }

        public static void Reset()
        {
            _services.Clear();
        }
    }
}
