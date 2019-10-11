using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using System;

namespace Autofac.AspNetCore.Extensions
{
    public static class TenantConfig
    {
        public static IConfiguration BuildTenantAppConfiguration(IConfiguration configRoot, IHostingEnvironment environment, string tenantId, Action<TenantBuilderContext, IConfigurationBuilder> configureAppConfiguration = null)
        {
            var tenantConfigBuilder = new ConfigurationBuilder()
            .SetBasePath(environment.ContentRootPath)
            .AddJsonFile($"appsettings.{tenantId}.json", optional: true, reloadOnChange: true)
            .AddJsonFile($"appsettings.{tenantId}.{environment.EnvironmentName}.json", optional: true, reloadOnChange: true);

            var tenantConfig = tenantConfigBuilder.Build() as ConfigurationRoot;

            //Final tenant config
            var finalTenantConfigBuilder = new ConfigurationBuilder();
            finalTenantConfigBuilder.AddConfiguration(configRoot);
            finalTenantConfigBuilder.AddConfiguration(tenantConfig);
            var finalTenantConfig = finalTenantConfigBuilder.Build() as ConfigurationRoot;

            if(configureAppConfiguration != null)
            {
                var context = new TenantBuilderContext(tenantId)
                {
                    Configuration = finalTenantConfig,
                    HostingEnvironment = environment
                };

                configureAppConfiguration(context, tenantConfigBuilder);

                tenantConfig.Dispose();
                finalTenantConfig.Dispose();

                tenantConfig = tenantConfigBuilder.Build() as ConfigurationRoot;

                //Final tenant config
                finalTenantConfigBuilder = new ConfigurationBuilder();
                finalTenantConfigBuilder.AddConfiguration(configRoot);
                finalTenantConfigBuilder.AddConfiguration(tenantConfig);
                finalTenantConfig = finalTenantConfigBuilder.Build() as ConfigurationRoot;
            }

            return finalTenantConfig;
        }
    }
}
