using Autofac.Multitenant;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;

namespace Autofac.AspNetCore.Extensions
{
    public class CompositeTenantIdentificationStrategy : ITenantIdentificationStrategy
    {
        private readonly IEnumerable<ITenantIdentificationStrategy> _identificationStrategies;

        public CompositeTenantIdentificationStrategy(IHttpContextAccessor accessor, IEnumerable<ITenantIdentificationStrategy> identificationStrategies)
        {
            _identificationStrategies = identificationStrategies;
            Accessor = accessor;
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

            foreach (var identificationStrategy in _identificationStrategies)
            {
                identificationStrategy.TryIdentifyTenant(out tenantId);

                if (tenantId != null)
                {
                    context.Items["_tenantId"] = tenantId;
                    return true;
                }
                else
                    context.Items.Remove("_tenantId");

            }

            tenantId = null;
            context.Items["_tenantId"] = null;
            return false;
        }
    }
}
