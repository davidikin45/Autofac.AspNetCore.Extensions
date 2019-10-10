using Autofac.Multitenant;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Autofac.AspNetCore.Extensions.Middleware
{
    public class Tenant404Middleware
    {
        private readonly RequestDelegate _next;
        private readonly AutofacMultitenantOptions _options;

        public Tenant404Middleware(RequestDelegate next, AutofacMultitenantOptions options)
        {
            this._next = next;
            _options = options;
        }

        public Task InvokeAsync(HttpContext context)
        {
            string tenantId;
            var valid = context.TryGetRequestTenantId(out tenantId);

            var mtc = context.RequestServices.GetRequiredService<MultitenantContainer>();

            if (!valid ||(tenantId == null && !_options.AllowDefaultTenantRequests) || (tenantId != null && !mtc.GetTenants().ToList().Any(t => string.Equals(t as string, tenantId, StringComparison.OrdinalIgnoreCase))))
            {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                return Task.CompletedTask;
            }

            return this._next(context);
        }
    }
}
