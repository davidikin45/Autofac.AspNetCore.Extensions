using Autofac.AspNetCore.Extensions.Middleware;
using Autofac.AspNetCore.Extensions.Mvc;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Autofac.AspNetCore.Extensions
{
    public static class MultitenantExtensions
    {
        public static IEnumerable<string> GetTenants(this IConfiguration configuration)
        {
            return configuration.GetSection("Tenants").Get<string[]>() ?? new string[] { };
        }

        public static string GetTenantId(this HttpContext context)
        {
            return context.Items.ContainsKey("_tenantId") ? context.Items["_tenantId"] != null ? context.Items["_tenantId"].ToString() : null : null;
        }

        public static IMvcBuilder AddHangfireTenantViewLocations(this IMvcBuilder builder)
        {
            builder.Services.Configure<RazorViewEngineOptions>(options =>
            {
                if (!(options.ViewLocationExpanders.FirstOrDefault() is TenantViewLocationExpander))
                {
                    options.ViewLocationExpanders.Insert(0, new TenantViewLocationExpander());
                }
            });

            return builder;
        }

        public static IServiceCollection AddTenantConfiguration(this IServiceCollection services, Assembly assembly)
        {
            var types = assembly
                .GetExportedTypes()
                .Where(type => typeof(ITenantConfiguration).IsAssignableFrom(type))
                .Where(type => (type.IsAbstract == false) && (type.IsInterface == false));

            foreach (var type in types)
            {
                services.AddSingleton(typeof(ITenantConfiguration), type);
            }

            return services;
        }

        public static IServiceCollection AddTenantConfiguration(this IServiceCollection services)
        {
            var target = Assembly.GetEntryAssembly();
            return services.AddTenantConfiguration(target);
        }
        public static IApplicationBuilder UseTenant404Middleware(this IApplicationBuilder buildr)
        {
            return buildr.UseMiddleware<Tenant404Middleware>();
        }
    }
}
