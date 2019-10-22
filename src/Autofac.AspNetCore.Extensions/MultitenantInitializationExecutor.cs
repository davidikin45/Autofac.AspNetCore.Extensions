using Autofac.Extensions.DependencyInjection;
using Autofac.Multitenant;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Autofac.AspNetCore.Extensions
{
    public class MultitenantInitializationExecutor
    {
        private readonly ILogger<MultitenantInitializationExecutor> _logger;
        private readonly MultitenantContainer _mtc;
        private readonly AutofacMultitenantOptions _options;
        private readonly IEnumerable<ITenantConfiguration> _tenantConfigurations;

        public MultitenantInitializationExecutor(ILogger<MultitenantInitializationExecutor> logger, IServiceProvider serviceProvider, AutofacMultitenantOptions options, IEnumerable<ITenantConfiguration> tenantConfigurations)
        {
            _logger = logger;
            _mtc = serviceProvider.GetRequiredService<MultitenantContainer>();
            _options = options;
            _tenantConfigurations = tenantConfigurations;
        }

        public async Task InitializeAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting tenant initialization");

            try
            {
                foreach (var kvp in _options.Tenants)
                {
                    _logger.LogInformation("Starting initialization for {tenant}", kvp.Key);

                    var tenantConfiguration = _tenantConfigurations.FirstOrDefault(i => i.TenantId.ToString() == kvp.Key);

                    var defaultBuilder = new TenantBuilder(kvp.Key);
                    foreach (var configureTenantsDelegate in _options.ConfigureTenantsDelegates)
                    {
                        configureTenantsDelegate(defaultBuilder);
                    }

                    var tenantBuilder = new TenantBuilder(kvp.Key);
                    if (kvp.Value != null)
                        kvp.Value(tenantBuilder);

                    var options = new TenantBuilder(kvp.Key)
                    {
                        ConfigureAppConfigurationDelegate = (context, builder) =>
                        {
                            defaultBuilder.ConfigureAppConfigurationDelegate(context, builder);
                            tenantBuilder.ConfigureAppConfigurationDelegate(context, builder);
                            if (tenantConfiguration != null)
                                tenantConfiguration.ConfigureAppConfiguration(context, builder);
                        },
                        ConfigureServicesDelegate = (context, services) =>
                        {
                            defaultBuilder.ConfigureServicesDelegate(context, services);
                            tenantBuilder.ConfigureServicesDelegate(context, services);
                            if (tenantConfiguration != null)
                                tenantConfiguration.ConfigureServices(context, services);
                        },
                        InitializeDbAsyncDelegate = async (sp, ct) =>
                        {
                            await defaultBuilder.InitializeDbAsyncDelegate(sp, ct);
                            await tenantBuilder.InitializeDbAsyncDelegate(sp, ct);
                            if (tenantConfiguration != null)
                                await tenantConfiguration.InitializeDbAsync(sp, ct);
                        },
                        InitializeAsyncDelegate = async (sp, ct) =>
                        {
                            await defaultBuilder.InitializeAsyncDelegate(sp, ct);
                            await tenantBuilder.InitializeAsyncDelegate(sp, ct);
                            if (tenantConfiguration != null)
                                await tenantConfiguration.InitializeAsync(sp, ct);
                        },
                    };

                    var serviceProvider = new AutofacServiceProvider(_mtc.GetTenantScope(kvp.Key));

                    try
                    {
                        await options.InitializeDbAsyncDelegate(serviceProvider, cancellationToken);
                        await options.InitializeAsyncDelegate(serviceProvider, cancellationToken);

                        _logger.LogInformation("Initialization for {tenant} completed", kvp.Key);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Initialization for {tenant} failed", kvp.Key);
                        throw;
                    }
                }

                _logger.LogInformation("tenant initialization completed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "tenant initialization failed");
                throw;
            }
        }
    }
}
