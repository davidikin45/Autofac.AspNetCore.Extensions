using Autofac.Extensions.DependencyInjection;
using Autofac.Multitenant;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Autofac.AspNetCore.Extensions
{
    public class TenantInitializationExecutor
    {
        private readonly ILogger<TenantInitializationExecutor> _logger;
        private readonly MultitenantContainer _mtc;
        private readonly AutofacMultitenantOptions _options;

        public TenantInitializationExecutor(ILogger<TenantInitializationExecutor> logger, MultitenantContainer mtc, AutofacMultitenantOptions options)
        {
            _logger = logger;
            _mtc = mtc;
            _options = options;
        }

        public async Task InitializeAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting tenant initialization");

            try
            {
                foreach (var kvp in _options.Tenants)
                {
                    _logger.LogInformation("Starting initialization for {tenant}", kvp.Key);

                    var defaultBuilder = new TenantBuilder();
                    if (_options.ConfigureTenantsDelegate != null)
                        _options.ConfigureTenantsDelegate(defaultBuilder);

                    var tenantBuilder = new TenantBuilder();
                    if (kvp.Value != null)
                        kvp.Value(tenantBuilder);

                    var options = new TenantBuilder()
                    {
                        ConfigureAppConfigurationDelegate = (context, builder) =>
                        {
                            defaultBuilder.ConfigureAppConfigurationDelegate(context, builder);
                            tenantBuilder.ConfigureAppConfigurationDelegate(context, builder);
                        },
                        ConfigureServicesDelegate = (context, services) =>
                        {
                            defaultBuilder.ConfigureServicesDelegate(context, services);
                            tenantBuilder.ConfigureServicesDelegate(context, services);
                        },
                        InitializeDbAsyncDelegate = async (sp, ct) =>
                        {
                            await defaultBuilder.InitializeDbAsyncDelegate(sp, ct);
                            await tenantBuilder.InitializeDbAsyncDelegate(sp, ct);
                        },
                        InitializeAsyncDelegate = async (sp, ct) =>
                        {
                            await defaultBuilder.InitializeAsyncDelegate(sp, ct);
                            await tenantBuilder.InitializeAsyncDelegate(sp, ct);
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
