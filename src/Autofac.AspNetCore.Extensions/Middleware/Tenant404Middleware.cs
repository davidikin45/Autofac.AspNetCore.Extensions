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

        public Tenant404Middleware(RequestDelegate next)
        {
            this._next = next;
        }

        public Task InvokeAsync(HttpContext context)
        {
            var tenantId = context.GetTenantId();

            if(tenantId == null)
            {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                return Task.CompletedTask;
            }

            return this._next(context);
        }
    }
}
