using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using System;
using static Autofac.AspNetCore.Extensions.AutofacServiceCollectionExtensions;

namespace Autofac.AspNetCore.Extensions
{
    public static class AutofacWHostBuilderExtensions
    {
        /// <summary>
        /// Uses the autofac container.
        /// </summary>
        public static IHostBuilder UseAutofac(this IHostBuilder builder, Action<AutofacOptions> setupAction = null)
        {
            var options = new AutofacOptions();
            if (setupAction != null)
                setupAction(options);

            return builder.UseServiceProviderFactory(new AutofacServiceProviderFactory(options));
        }

        /// <summary>
        /// Uses the autofac multi tenant container.
        /// </summary>
        public static IHostBuilder UseAutofacMultiTenant(this IHostBuilder builder, Action<AutofacMultitenantOptions> setupAction = null)
        {
            var options = new AutofacMultitenantOptions();
            if (setupAction != null)
                setupAction(options);

            builder.UseServiceProviderFactory(new AutofacMultiTenantServiceProviderFactory(options));

            return builder.ConfigureServices((context, services) =>
            {
                //RequestServicesContainerMiddleware
                //When using the WebHostBuilder the UseAutofacMultitenantRequestServices extension is used to tie the multitenant container to the request lifetime scope generation process.\\ASP.NET Core default RequestServicesContainerMiddleware is where the per-request lifetime scope usually gets generated. However, its constructor is where it wants the IServiceScopeFactory that will be used later during the request to create the request lifetime scope.

                //Unfortunately, that means the IServiceScopeFactory is created / resolved at the point when the request comes in, long before an HttpContext is set in any IHttpContextAccessor. The result is the scope factory ends up coming from the default tenant scope, before a tenant can be identified, and per-request services will later all come from the default tenant.Multitenancy fails.
                //This package provides a different request services middleware that ensures the IHttpContextAccessor.HttpContext is set and defers creation of the request lifetime scope until as late as possible so anything needed for tenant identification can be established.
                //Adds IHttpContextAccessor
                services.AddAutofacMultitenantRequestServices();
            });
        }
    }
}
