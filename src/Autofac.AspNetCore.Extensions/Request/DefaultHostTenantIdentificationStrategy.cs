using Autofac.Multitenant;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;

namespace Autofac.AspNetCore.Extensions
{
    public class DefaultHostTenantIdentificationStrategy : ITenantIdentificationStrategy
    {
        private readonly ILogger<DefaultHostTenantIdentificationStrategy> _logger;
        private readonly AutofacMultitenantOptions _options;
        public DefaultHostTenantIdentificationStrategy(IHttpContextAccessor accessor, ILogger<DefaultHostTenantIdentificationStrategy> logger, AutofacMultitenantOptions options)
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

            var tenantSlug = GetHostFromRequest(context);

            string temp;
            if (TryMapHostToTenantId(context, tenantSlug, out temp))
            {
                context.Items["_requestTenantId"] = temp;

                tenantId = MapTenantIdToContainerId(context, temp);
                context.Items["_tenantId"] = tenantId;

                this._logger.LogInformation("Identified tenant from host: {tenant}", temp);
                return true;
            }

            this._logger.LogDebug("Unable to identify tenant from host.");
            tenantId = null;

            context.Items["_tenantId"] = null;

            return false;
        }

        public virtual HostString GetHostFromRequest(HttpContext context)
        {
            return context.Request.Host;
        }

        public virtual bool TryMapHostToTenantId(HttpContext context, HostString hostString ,out string tenantId)
        {
            //destination
            var host = hostString.Value.Replace("www.", "");
            var hostWithoutPort = host.Split(':')[0];
            var hostSplit = hostWithoutPort.Split('.');
            var subdomain = false;
            if ((hostSplit.Last() == "localhost" && hostSplit.Count() > 1) || (hostSplit.Last() != "localhost" && hostSplit.Count() > 2))
            {
                subdomain = true;
            }

            Func<string, bool> exactMatchHostWithPortCondition = h => string.Equals(h, host, StringComparison.OrdinalIgnoreCase);
            Func<string, bool> exactMatchHostWithoutPortCondition = h => string.Equals(h, hostWithoutPort, StringComparison.OrdinalIgnoreCase);
            Func<string, bool> nonSubdomainCondition = h => h == "" && !subdomain;
            Func<string, bool> endWildcardCondition = h => h.EndsWith("*") && host.StartsWith(h.Replace("*", ""));
            Func<string, bool> startWildcardWithPortCondition = h => h.StartsWith("*") && host.EndsWith(h.Replace("*", ""));
            Func<string, bool> startWildcardCondition = h => h.StartsWith("*") && hostWithoutPort.EndsWith(h.Replace("*", ""));

            var mappings = _options.HostMappings.Keys.ToList();

            var exactMatchHostWithPort = mappings.Where(exactMatchHostWithPortCondition).ToList();
            var exactMatchHostWithoutPort = mappings.Where(exactMatchHostWithoutPortCondition).ToList();
            var nonSubdomain = mappings.Where(nonSubdomainCondition).ToList();

            var endWildcard = mappings.Where(endWildcardCondition).ToList();
            var startWildcardWithPort = mappings.Where(startWildcardWithPortCondition).ToList();
            var startWildcard = mappings.Where(startWildcardCondition).ToList();

            if (exactMatchHostWithPort.Count > 0)
            {
                if (exactMatchHostWithPort.Count == 1)
                {
                    tenantId = _options.HostMappings[exactMatchHostWithPort.First()];
                    return true;
                }
            }
            else if (exactMatchHostWithoutPort.Count() > 0)
            {
                if (exactMatchHostWithoutPort.Count == 1)
                {
                    tenantId = _options.HostMappings[exactMatchHostWithoutPort.First()];
                    return true;
                }
            }
            else if (nonSubdomain.Count > 0)
            {
                tenantId = _options.HostMappings[nonSubdomain.First()];
                return true;
            }
            else if (endWildcard.Count > 0)
            {
                tenantId = _options.HostMappings[endWildcard.OrderByDescending(h => h.Length).First()];
                return true;
            }
            else if (startWildcardWithPort.Count > 0)
            {
                tenantId = _options.HostMappings[startWildcardWithPort.OrderByDescending(h => h.Length).First()];
                return true;
            }
            else if (startWildcard.Count > 0)
            {
                tenantId = _options.HostMappings[startWildcard.OrderByDescending(h => h.Length).First()];
                return true;
            }

            tenantId = null;
            return false;
        }

        public virtual string MapTenantIdToContainerId(HttpContext context, string tenantId)
        {
            //null > new DefaultTenantId()
            //Anything not null will get used/added

            if (tenantId == null)
                return null;

            var mtc = context.RequestServices.GetRequiredService<MultitenantContainer>();
            return mtc.GetTenants().ToList().Any(t => string.Equals(t as string, tenantId, StringComparison.OrdinalIgnoreCase)) ? tenantId : null;
        }
    }
}
