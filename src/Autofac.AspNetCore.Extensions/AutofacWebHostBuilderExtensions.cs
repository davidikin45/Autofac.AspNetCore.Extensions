using Autofac.Multitenant;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using System;
using static Autofac.AspNetCore.Extensions.AutofacServiceCollectionExtensions;

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
            MultitenantContainer multiTenantContainer = null;
            Func<MultitenantContainer> multitenantContainerAccessor = () => multiTenantContainer;
            Action<MultitenantContainer> multitenantContainerSetter = (mtc) => { multiTenantContainer = mtc; };
            builder.ConfigureServices(services => services.AddAutofacMultitenant(multitenantContainerSetter, setupAction));
            builder.ConfigureServices(services => services.AddSingleton((sp) => multiTenantContainer));
            return builder.UseAutofacMultitenantRequestServices(multitenantContainerAccessor);
        }
    }
}
