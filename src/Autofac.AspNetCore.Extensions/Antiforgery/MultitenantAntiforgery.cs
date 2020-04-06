using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using System.Threading.Tasks;

namespace Autofac.AspNetCore.Extensions.Antiforgery
{
    public class MultitenantAntiforgery : IAntiforgery
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly Type _type;
        public MultitenantAntiforgery(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;

            _type = typeof(IAntiforgery).Assembly.GetType("Microsoft.AspNetCore.Antiforgery.DefaultAntiforgery");

            //.NET Core 2.2
            //_type = typeof(DefaultAntiforgery);

        }

        private object GetTokenStore(HttpContext httpContext)
        {
            if (httpContext.Items.ContainsKey("_multitenantTokenStore"))
                return httpContext.Items["_multitenantTokenStore"];

            var options = httpContext.RequestServices.GetRequiredService<IOptions<AntiforgeryOptions>>();

            var type = typeof(AntiforgeryOptions).Assembly.GetType("Microsoft.AspNetCore.Antiforgery.DefaultAntiforgeryTokenStore");
            var interfaceType = typeof(AntiforgeryOptions).Assembly.GetType("Microsoft.AspNetCore.Antiforgery.IAntiforgeryTokenStore");
            var tokenStore = ActivatorUtilities.CreateInstance(_serviceProvider, type, options);

            //.NET Core 2.2
            //var tokenStore = new DefaultAntiforgeryTokenStore(options) as IAntiforgeryTokenStore;

            httpContext.Items["_multitenantTokenStore"] = tokenStore;

            return tokenStore;
        }

        private IAntiforgery GetAntiForgery(HttpContext httpContext)
        {
            if (httpContext.Items.ContainsKey("_multitenantDefaultAntiforgery"))
                return httpContext.Items["_multitenantDefaultAntiforgery"] as IAntiforgery;

            var options = httpContext.RequestServices.GetRequiredService<IOptions<AntiforgeryOptions>>();
            var tokenStore = GetTokenStore(httpContext);

            var antiForgery = ActivatorUtilities.CreateInstance(_serviceProvider, _type, options, tokenStore) as IAntiforgery;
            httpContext.Items["_multitenantDefaultAntiforgery"] = antiForgery;

            return antiForgery;
        }

        public AntiforgeryTokenSet GetAndStoreTokens(HttpContext httpContext)
        {
            return GetAntiForgery(httpContext).GetAndStoreTokens(httpContext);
        }

        public AntiforgeryTokenSet GetTokens(HttpContext httpContext)
        {
            return GetAntiForgery(httpContext).GetTokens(httpContext);
        }

        public Task<bool> IsRequestValidAsync(HttpContext httpContext)
        {
            return GetAntiForgery(httpContext).IsRequestValidAsync(httpContext);
        }

        public void SetCookieTokenAndHeader(HttpContext httpContext)
        {
            GetAntiForgery(httpContext).SetCookieTokenAndHeader(httpContext);
        }

        public Task ValidateRequestAsync(HttpContext httpContext)
        {
            return GetAntiForgery(httpContext).ValidateRequestAsync(httpContext);
        }
    }
}
