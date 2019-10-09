using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Concurrent;

namespace Autofac.AspNetCore.Extensions.OptionsCache
{
    //IOptionsMonitor
    //IOptionsChangeTokenSource are registered against IConfiguration
    public class MultitenantOptionsMonitorCache<TOptions> : IOptionsMonitorCache<TOptions> where TOptions : class
    {
        private readonly ConcurrentDictionary<string, Lazy<IOptionsMonitorCache<TOptions>>> _cache = new ConcurrentDictionary<string, Lazy<IOptionsMonitorCache<TOptions>>>();

        private readonly IHttpContextAccessor _httpContextAccessor;

        public MultitenantOptionsMonitorCache(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
            _cache.TryAdd("root", new Lazy<IOptionsMonitorCache<TOptions>>(() => new OptionsCache<TOptions>()));
        }

        public TOptions GetOrAdd(string name, Func<TOptions> createOptions) => GetCache.Value.GetOrAdd(name, createOptions);

        public bool TryAdd(string name, TOptions options) => GetCache.Value.TryAdd(name, options);

        public bool TryRemove(string name) => GetCache.Value.TryRemove(name);

        public void Clear() => GetCache.Value.Clear();

        private Lazy<IOptionsMonitorCache<TOptions>> GetCache => _cache.GetOrAdd(_httpContextAccessor.HttpContext?.GetTenantId() ?? "root", (tenant) => new Lazy<IOptionsMonitorCache<TOptions>>(() => new OptionsCache<TOptions>()));
    }
}
