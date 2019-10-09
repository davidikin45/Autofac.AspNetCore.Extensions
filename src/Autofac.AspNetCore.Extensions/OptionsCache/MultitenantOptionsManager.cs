using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Concurrent;

namespace Autofac.AspNetCore.Extensions.OptionsCache
{
    public class MultitenantOptionsManager<TOptions> : IOptions<TOptions>, IOptionsSnapshot<TOptions> where TOptions : class, new()
    {
        private readonly ConcurrentDictionary<string, Lazy<OptionsManager<TOptions>>> _cache = new ConcurrentDictionary<string, Lazy<OptionsManager<TOptions>>>(StringComparer.Ordinal);

        private readonly IHttpContextAccessor _httpContextAccessor;

        public MultitenantOptionsManager(IOptionsFactory<TOptions> factory, IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
            _cache.TryAdd("root", new Lazy<OptionsManager<TOptions>>(() => new OptionsManager<TOptions>(factory)));
        }

        public TOptions Value => Get(Options.DefaultName);

        public TOptions Get(string name)
        {
            return GetCache().Value.Get(name);
        }

        private Lazy<OptionsManager<TOptions>> GetCache() {
            var optionsFactory = _httpContextAccessor.HttpContext?.RequestServices.GetRequiredService<IOptionsFactory<TOptions>>();
            return _cache.GetOrAdd(_httpContextAccessor.HttpContext?.GetTenantId() ?? "root", (tenantId) => new Lazy<OptionsManager<TOptions>>(() => new OptionsManager<TOptions>(optionsFactory)));
        }
    }
}
