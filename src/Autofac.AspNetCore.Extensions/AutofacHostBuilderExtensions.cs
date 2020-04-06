using Autofac.AspNetCore.Extensions.Antiforgery;
using Autofac.AspNetCore.Extensions.Data;
using Autofac.AspNetCore.Extensions.Middleware;
using Autofac.AspNetCore.Extensions.OptionsCache;
using Autofac.AspNetCore.Extensions.Security;
using Autofac.AspNetCore.Extensions.TempData;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System;
using static Autofac.AspNetCore.Extensions.AutofacServiceCollectionExtensions;

namespace Autofac.AspNetCore.Extensions
{
    public static class AutofacWHostBuilderExtensions
    {
#if NETCOREAPP3_0
        public static IHostBuilder UseAutofac(this IHostBuilder builder) => builder.UseAutofac((options) => { });

        /// <summary>
        /// Uses the autofac container.
        /// </summary>
        public static IHostBuilder UseAutofac(this IHostBuilder builder, Action<AutofacOptions> configure) => builder.UseAutofac((context, options) => configure(options));

        /// <summary>
        /// Uses the autofac container.
        /// </summary>
        public static IHostBuilder UseAutofac(this IHostBuilder builder, Action<HostBuilderContext, AutofacOptions> configure)
        {
            return builder.UseServiceProviderFactory(context =>
            {
                var options = new AutofacOptions();
                if (configure != null)
                    configure(context, options);

                return new AutofacServiceProviderFactory(options)
                ;
            });
        }

        public static IHostBuilder UseAutofacMultitenant(this IHostBuilder builder) => builder.UseAutofacMultitenant((options) => { });

        /// <summary>
        /// Uses the autofac multi tenant container.
        /// </summary>
        public static IHostBuilder UseAutofacMultitenant(this IHostBuilder builder, Action<AutofacMultitenantOptions> configure) => builder.UseAutofacMultitenant((context, options) => configure(options));

        /// <summary>
        /// Uses the autofac multi tenant container.
        /// </summary>
        public static IHostBuilder UseAutofacMultitenant(this IHostBuilder builder, Action<HostBuilderContext, AutofacMultitenantOptions> configure)
        {
            AutofacMultitenantOptions options = null;

            builder.UseServiceProviderFactory(context =>
            {
                return new AutofacMultiTenantServiceProviderFactory(options);
            });

            return builder.ConfigureServices((context, services) =>
            {
                services.AddTransient<IPostConfigureOptions<RazorPagesOptions>, RazorPagesOptionsSetup>();

                var tenant = new Tenant(string.Empty, context.Configuration);
                services.AddSingleton<ITenant>(tenant);

                services.AddTransient<ITenantService, MultiTenantService>();
                services.AddTransient<ITenantDbContextStrategyService, MultiTenantDbContextStrategyService>();

                services.AddTenantViewLocations();

                services.AddTenantConfiguration();
                //RequestServicesContainerMiddleware
                //When using the WebHostBuilder the UseAutofacMultitenantRequestServices extension is used to tie the multitenant container to the request lifetime scope generation process.\\ASP.NET Core default RequestServicesContainerMiddleware is where the per-request lifetime scope usually gets generated. However, its constructor is where it wants the IServiceScopeFactory that will be used later during the request to create the request lifetime scope.

                //Unfortunately, that means the IServiceScopeFactory is created / resolved at the point when the request comes in, long before an HttpContext is set in any IHttpContextAccessor. The result is the scope factory ends up coming from the default tenant scope, before a tenant can be identified, and per-request services will later all come from the default tenant.Multitenancy fails.
                //This package provides a different request services middleware that ensures the IHttpContextAccessor.HttpContext is set and defers creation of the request lifetime scope until as late as possible so anything needed for tenant identification can be established.
                //Adds IHttpContextAccessor
                services.AddAutofacMultitenantRequestServices();

                options = new AutofacMultitenantOptions();
                if (configure != null)
                    configure(context, options);

                foreach (var configureServices in options.ConfigureServicesList)
                {
                    configureServices(services);
                }

                if(options.ConfigureStaticFilesDelegate != null)
                {
                    services.ConfigureMultitenantStaticFilesRewriteOptions(options.ConfigureStaticFilesDelegate);
                }

                services.AddSingleton<IStartupFilter, Tenant404StartupFilter>();

                //services.AddSingleton<IStartupFilter, TenantStaticFilesRewriteStartupFilter>();

                services.AddSingleton(sp => options);
                services.AddTransient<MultitenantInitializationExecutor>();
                services.AddHostedService<MultitenantInitializationHostedService>();

                //https://github.com/aspnet/Extensions/blob/90476ca2be4bd7d32dbf47ffbccf0371b58c67b7/src/Options/Options/src/OptionsFactory.cs
                //services.TryAdd(ServiceDescriptor.Transient(typeof(IOptionsFactory<>), typeof(OptionsFactory<>)));

                //https://github.com/aspnet/Extensions/blob/90476ca2be4bd7d32dbf47ffbccf0371b58c67b7/src/Options/Options/src/OptionsManager.cs
                //services.TryAdd(ServiceDescriptor.Singleton(typeof(IOptions<>), typeof(OptionsManager<>))); Factory
                //services.TryAdd(ServiceDescriptor.Scoped(typeof(IOptionsSnapshot<>), typeof(OptionsManager<>))); F

                //https://github.com/aspnet/Extensions/blob/90476ca2be4bd7d32dbf47ffbccf0371b58c67b7/src/Options/Options/src/OptionsMonitor.cs
                //services.TryAdd(ServiceDescriptor.Singleton(typeof(IOptionsMonitor<>), typeof(OptionsMonitor<>))); F OC
                //https://github.com/aspnet/Extensions/blob/730c5d93b2e3f2dc0f045c90e79ad9443ea169da/src/Options/Options/src/OptionsCache.cs
                //services.TryAdd(ServiceDescriptor.Singleton(typeof(IOptionsMonitorCache<>), typeof(OptionsCache<>)));

                services.AddTransient(typeof(IOptionsFactory<>), typeof(MultitenantOptionsFactory<>));

                services.AddSingleton(typeof(IOptions<>), typeof(MultitenantOptionsManager<>));
                services.AddScoped(typeof(IOptionsSnapshot<>), typeof(MultitenantOptionsManager<>));
                services.AddSingleton(typeof(IOptionsMonitor<>), typeof(MultitenantOptionsMonitor<>));
                services.AddSingleton(typeof(IOptionsMonitorCache<>), typeof(MultitenantOptionsMonitorCache<>));

                //AuthenticationHandlerProvider
                //JwtBearerHandler - Transient
                //IOptionsMonitor<T>
                services.TryAddEnumerable(ServiceDescriptor.Singleton<IPostConfigureOptions<JwtBearerOptions>, JwtBearerMultitenantPostConfigureOptions>());

                //AuthenticationHandlerProvider
                //CookieAuthenticationHandler - Transient
                //IOptionsMonitor<T>
                services.TryAddEnumerable(ServiceDescriptor.Singleton<IPostConfigureOptions<CookieAuthenticationOptions>, CookieAuthenticationMultitenantPostConfigureOptions>());

                //Antitforgery
                //IHtmlGenerator - Singleton
                //IAntiForgery - Singleton
                //IOptions<AntiforgeryOptions> - Singleton
                services.AddSingleton<IAntiforgery, MultitenantAntiforgery>();
                services.TryAddEnumerable(ServiceDescriptor.Singleton<IPostConfigureOptions<AntiforgeryOptions>, AntiForgeryMultitenantPostConfigureOptions>());
                services.AddSingleton<IAntiforgeryAdditionalDataProvider, AntiForgeryMultitenantAdditionalData>();

                //TempData
                //ViewResultExecutor - Singleton
                //ITempDataDictionaryFactory - Singleton
                //CookieTempDataProvider - Singleton
                //IOptions<CookieTempDataProviderOptions> - Singleton
                services.AddSingleton<ITempDataProvider, MultitenantCookieTempDataProvider>();
                services.TryAddEnumerable(ServiceDescriptor.Singleton<IPostConfigureOptions<CookieTempDataProviderOptions>, CookieTempDataPostConfigureOptions>());

                //SessionMiddleware
                //IOptions<SessionOptions> - Singleton
                services.TryAddEnumerable(ServiceDescriptor.Singleton<IPostConfigureOptions<SessionOptions>, SessionPostConfigureOptions>());
            });
        }
#endif
    }
}
