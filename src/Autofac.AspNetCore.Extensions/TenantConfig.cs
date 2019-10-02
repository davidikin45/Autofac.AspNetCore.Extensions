using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Autofac.AspNetCore.Extensions
{
    public static class TenantConfig
    {
        public static IConfiguration BuildTenantAppConfiguration(IConfiguration configRoot, IHostingEnvironment environment, string tenantId, Action<TenantBuilderContext, IConfigurationBuilder> configureAppConfiguration = null)
        {
            var tenantConfigBuilder = new ConfigurationBuilder();

            var appSettingsFileName = $"appsettings.{tenantId}.json";
            var appSettingsEnvironmentFilename = $"appsettings.{tenantId}.{environment.EnvironmentName}.json";

            tenantConfigBuilder
            .SetBasePath(environment.ContentRootPath)
            .AddJsonFile(appSettingsFileName, optional: true, reloadOnChange: true)
            .AddJsonFile(appSettingsEnvironmentFilename, optional: true, reloadOnChange: true);

            var tenantConfig = tenantConfigBuilder.Build() as ConfigurationRoot;
            var tenantConfigProviders = tenantConfig.Providers as List<IConfigurationProvider>;

            //Final tenant config
            var finalTenantConfigBuilder = new ConfigurationBuilder();
            var finalTenantConfig = finalTenantConfigBuilder.Build() as ConfigurationRoot;

            var finalTenantConfigProviders = finalTenantConfig.Providers as List<IConfigurationProvider>;

            finalTenantConfigProviders.AddRange((configRoot as ConfigurationRoot).Providers);
            finalTenantConfigProviders.AddRange(tenantConfigProviders);

            if(configureAppConfiguration != null)
            {
                var context = new TenantBuilderContext()
                {
                    Configuration = finalTenantConfig,
                    HostingEnvironment = environment
                };

                configureAppConfiguration(context, tenantConfigBuilder);

                tenantConfig = tenantConfigBuilder.Build() as ConfigurationRoot;
                tenantConfigProviders = tenantConfig.Providers as List<IConfigurationProvider>;

                //Final tenant config
                finalTenantConfigBuilder = new ConfigurationBuilder();
                finalTenantConfig = finalTenantConfigBuilder.Build() as ConfigurationRoot;

                finalTenantConfigProviders = finalTenantConfig.Providers as List<IConfigurationProvider>;

                finalTenantConfigProviders.AddRange((configRoot as ConfigurationRoot).Providers);

                if (tenantConfigProviders.Count() > 0)
                {
                    finalTenantConfigProviders.AddRange(tenantConfigProviders);
                }
            }

            return finalTenantConfig;
        }
    }
}
