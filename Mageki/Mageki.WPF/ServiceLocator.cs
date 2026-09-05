using System;
using Mageki.DependencyServices;

namespace Mageki.WPF
{
    /// <summary>
    /// Lightweight service locator — replaces Xamarin.Forms DependencyService.
    /// </summary>
    public static class ServiceLocator
    {
        private static readonly System.Collections.Generic.Dictionary<Type, object> _services =
            new();

        public static void Register<T>(T instance) => _services[typeof(T)] = instance!;

        public static T? Get<T>()
            where T : class => _services.TryGetValue(typeof(T), out var svc) ? (T)svc : null;
    }
}
