using Microsoft.AspNetCore.Hosting;
using System;

namespace Autofac.AspNetCore.Extensions
{
    public static class AutofacWebHostBuilderExtensions
    {
        /// <summary>
        /// Uses the autofac container.
        /// </summary>
        public static IWebHostBuilder UseAutofac(this IWebHostBuilder builder, Action<AutofacOptions> setupAction = null)
        {
            return builder.ConfigureServices(services => services.AddAutofac(setupAction));
        }

        /// <summary>
        /// Uses the autofac multi tenant container.
        /// </summary>
        public static IWebHostBuilder UseAutofacMultiTenant(this IWebHostBuilder builder, Action<AutofacMultitenantOptions> setupAction = null)
        {

            builder.ConfigureServices(services => services.AddAutofacMultitenant(setupAction));

            //RequestServicesContainerMiddleware
            //When using the WebHostBuilder the UseAutofacMultitenantRequestServices extension is used to tie the multitenant container to the request lifetime scope generation process.

            //Unfortunately, that means the IServiceScopeFactory is created / resolved at the point when the request comes in, long before an HttpContext is set in any IHttpContextAccessor. The result is the scope factory ends up coming from the default tenant scope, before a tenant can be identified, and per-request services will later all come from the default tenant.Multitenancy fails.
            //This package provides a different request services middleware that ensures the IHttpContextAccessor.HttpContext is set and defers creation of the request lifetime scope until as late as possible so anything needed for tenant identification can be established.
            //Adds IHttpContextAccessor
            return builder.UseAutofacMultitenantRequestServices();
        }
    }
}
