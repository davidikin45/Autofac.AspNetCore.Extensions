using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Autofac.AspNetCore.Extensions.OptionsCache
{
    //IOptionsMonitor
    //IOptionsChangeTokenSource are registered against IConfiguration
    public class MultitenantOptionsMonitor<TOptions> : IOptionsMonitor<TOptions> where TOptions : class, new()
    {
        private readonly ConcurrentDictionary<string, Lazy<IOptionsMonitor<TOptions>>> _cache = new ConcurrentDictionary<string, Lazy<IOptionsMonitor<TOptions>>>();

        private readonly IOptionsFactory<TOptions> _factory;
        private readonly IEnumerable<IOptionsChangeTokenSource<TOptions>> _sources;
        private readonly IOptionsMonitorCache<TOptions> _optionsCache;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public MultitenantOptionsMonitor(IOptionsFactory<TOptions> factory, IEnumerable<IOptionsChangeTokenSource<TOptions>> sources, IOptionsMonitorCache<TOptions> cache, IHttpContextAccessor httpContextAccessor)
        {
            _factory = factory;
            _sources = sources;
            _optionsCache = cache;
            _httpContextAccessor = httpContextAccessor;
            _cache.TryAdd("root", new Lazy<IOptionsMonitor<TOptions>>(() => new OptionsMonitor<TOptions>(_factory, _sources, new OptionsCache<TOptions>())));
        }

        public TOptions Get(string name) => GetCache.Value.Get(name);

        public IDisposable OnChange(Action<TOptions, string> listener) => GetCache.Value.OnChange(listener);

        private static FieldInfo ConfigurationChangeTokenSourceConfigurationField = typeof(ConfigurationChangeTokenSource<TOptions>).GetField("_config", BindingFlags.NonPublic | BindingFlags.Instance);

        private Lazy<IOptionsMonitor<TOptions>> GetCache => _cache.GetOrAdd(_httpContextAccessor.HttpContext?.GetTenantId() ?? "root", (tenant) => new Lazy<IOptionsMonitor<TOptions>>(() =>
        {
            var config = _httpContextAccessor.HttpContext.RequestServices.GetRequiredService<IConfiguration>();
            var sources = _httpContextAccessor.HttpContext.RequestServices.GetServices<IOptionsChangeTokenSource<TOptions>>().ToList();
            var count = sources.Count;
            
            for (int i = 0; i < count; i++)
            {
                var source = sources[i];
                if (source is ConfigurationChangeTokenSource<TOptions> configurationChangeTokenSource)
                {
                    var configurationSection = ConfigurationChangeTokenSourceConfigurationField.GetValue(source) as ConfigurationSection;
                    if (configurationSection != null)
                    {
                        var tenantSection = config.GetSection(configurationSection.Path);
                        if (tenantSection != null)
                        {
                            
                            sources.Add(new ConfigurationChangeTokenSource<TOptions>(configurationChangeTokenSource.Name, tenantSection));
                        }
                    }
                }
            }

            return new OptionsMonitor<TOptions>(
                 _httpContextAccessor.HttpContext.RequestServices.GetRequiredService<IOptionsFactory<TOptions>>(),
                 sources,
                 new OptionsCache<TOptions>()
                 );
        }));

        public TOptions CurrentValue => GetCache.Value.CurrentValue;
    }
}
