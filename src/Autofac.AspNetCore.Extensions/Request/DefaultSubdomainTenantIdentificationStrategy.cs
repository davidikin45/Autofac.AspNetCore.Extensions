using Autofac.Integration.AspNetCore.Multitenant;
using Autofac.Multitenant;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;

namespace Autofac.AspNetCore.Extensions
{
    public class DefaultSubdomainTenantIdentificationStrategy : ITenantIdentificationStrategy
    {
        private readonly ILogger<DefaultSubdomainTenantIdentificationStrategy> _logger;
        private readonly AutofacMultitenantOptions _options;
        public DefaultSubdomainTenantIdentificationStrategy(IHttpContextAccessor accessor, ILogger<DefaultSubdomainTenantIdentificationStrategy> logger, AutofacMultitenantOptions options)
        {
            this.Accessor = accessor;
            this._logger = logger;
            _options = options;
        }

        public IHttpContextAccessor Accessor { get; private set; }

        public bool TryIdentifyTenant(out object tenantId)
        {
            var context = this.Accessor.HttpContext;
            if (context == null)
            {
                // No current HttpContext. This happens during app startup
                // and isn't really an error, but is something to be aware of.
                tenantId = null;
                return false;
            }

            // Caching the value both speeds up tenant identification for
            // later and ensures we only see one log message indicating
            // relative success or failure for tenant ID.
            if (context.Items.TryGetValue("_tenantId", out tenantId))
            {
                // We've already identified the tenant at some point
                // so just return the cached value (even if the cached value
                // indicates we couldn't identify the tenant for this context).
                return tenantId != null;
            }

            var temp = MapTenantSlugToTenantId(context, GetTenantSlugFromRequest(context));
            if (temp != null)
            {
                tenantId = temp;
                context.Items["_tenantId"] = temp;
                this._logger.LogInformation("Identified tenant from host: {tenant}", tenantId);
                return true;
            }

            this._logger.LogWarning("Unable to identify tenant from host.");
            tenantId = null;
            context.Items["_tenantId"] = null;
            return false;
        }

        public virtual string GetTenantSlugFromRequest(HttpContext context)
        {
            var host = context.Request.Host.Value.Replace("www.", "");
            var hostWithoutPort = host.Split(':')[0];
            var hostSplit = hostWithoutPort.Split('.');

            if ((hostSplit.Length == 3 && hostSplit[2] != "localhost") || (hostSplit.Length == 2 && hostSplit[1] == "localhost"))
            {
                return hostSplit[0];
            }

            return null;
        }

        public virtual string MapTenantSlugToTenantId(HttpContext context, string tenantSlug)
        {
            tenantSlug = tenantSlug ?? string.Empty;
            var mtc = context.RequestServices.GetRequiredService<MultitenantContainer>();
            return mtc.GetTenants().Any(t => string.Equals(t as string, tenantSlug, StringComparison.OrdinalIgnoreCase)) ? tenantSlug.ToLowerInvariant() : null;
        }
    }
}
