using Autofac.Extensions.DependencyInjection;
using Autofac.Integration.AspNetCore.Multitenant;
using Autofac.Multitenant;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Autofac.AspNetCore.Extensions
{
    public static class AutofacServiceCollectionExtensions
    {
        public static IServiceCollection AddAutofac(this IServiceCollection services, Action<AutofacOptions> setupAction = null)
        {
            var options = new AutofacOptions();
            if (setupAction != null)
                setupAction(options);

            return services.AddSingleton<IServiceProviderFactory<ContainerBuilder>>(new AutofacServiceProviderFactory(options));
        }

        public class AutofacServiceProviderFactory : IServiceProviderFactory<ContainerBuilder>
        {
            private readonly AutofacOptions _options;

            public AutofacServiceProviderFactory(AutofacOptions options)
            {
                _options = options;
            }

            public ContainerBuilder CreateBuilder(IServiceCollection services)
            {
                var containerBuilder = new ContainerBuilder();

                containerBuilder.Populate(services);

                _options.ConfigureContainerDelegate(containerBuilder);

                return containerBuilder;
            }

            public IServiceProvider CreateServiceProvider(ContainerBuilder builder)
            {
                if (builder == null) throw new ArgumentNullException(nameof(builder));

                //IServiceProvider.GetAutofacRoot()
                var container = builder.Build();

                return new AutofacServiceProvider(container);
            }
        }

        public static IServiceCollection AddAutofacMultitenant(this IServiceCollection services, Action<AutofacMultitenantOptions> setupAction = null)
        {
            var options = new AutofacMultitenantOptions();
            if (setupAction != null)
                setupAction(options);

            services.AddTransient<TenantInitializationExecutor>();
            services.AddSingleton(options);

            return services.AddSingleton<IServiceProviderFactory<ContainerBuilder>>( sp => new AutofacMultiTenantServiceProviderFactory(sp.GetRequiredService<AutofacMultitenantOptions>()));
        }
        public class AutofacMultiTenantServiceProviderFactory : IServiceProviderFactory<ContainerBuilder>
        {
            private readonly AutofacMultitenantOptions _options;

            public AutofacMultiTenantServiceProviderFactory(AutofacMultitenantOptions options)
            {
                _options = options;
            }

            public ContainerBuilder CreateBuilder(IServiceCollection services)
            {
                var containerBuilder = new ContainerBuilder();

                containerBuilder.Populate(services);

                _options.ConfigureContainerDelegate(containerBuilder);

                return containerBuilder;
            }

            public IServiceProvider CreateServiceProvider(ContainerBuilder builder)
            {

                if (builder == null) throw new ArgumentNullException(nameof(builder));

                //AutofacMultitenantServiceProvider provider = null;

                // builder.Register(_ => provider)
                //.As<IServiceProvider>()
                //.ExternallyOwned();

                //IServiceProvider.GetAutofacMultitenantRoot()
                MultitenantContainer mtc = null;

                builder.Register(_ => mtc)
                .AsSelf()
                .ExternallyOwned();

                var container = builder.Build();

                var strategy = _options.TenantIdentificationStrategyDelegate(new AutofacServiceProvider(container));

                mtc = _options.CreateMultiTenantContainerDelegate (container, strategy);

                ConfigureTenants(mtc);

                var provider = new AutofacMultitenantServiceProvider(mtc);

                return provider;
            }

            private void ConfigureTenants(MultitenantContainer mtc)
            {
                var serviceProvider = new AutofacServiceProvider(mtc);

                var hostingEnvironment = serviceProvider.GetRequiredService<Microsoft.AspNetCore.Hosting.IHostingEnvironment>();
                var config = serviceProvider.GetRequiredService<IConfiguration>();

                foreach (var kvp in _options.Tenants)
                {
                    var actionBuilder = new ConfigurationActionBuilder();
                    var tenantServices = new ServiceCollection();

                    var defaultBuilder = new TenantBuilder();
                    if (_options.ConfigureTenantsDelegate != null)
                        _options.ConfigureTenantsDelegate(defaultBuilder);

                    var tenantBuilder = new TenantBuilder();
                    if (kvp.Value != null)
                        kvp.Value(tenantBuilder);

                    var options = new TenantBuilder() {
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
                        InitializeDbAsyncDelegate = async (sp, cancellationToken) =>
                     {
                         await defaultBuilder.InitializeDbAsyncDelegate(sp, cancellationToken);
                         await tenantBuilder.InitializeDbAsyncDelegate(sp, cancellationToken);
                     },
                        InitializeAsyncDelegate = async (sp, cancellationToken) =>
                     {
                         await defaultBuilder.InitializeAsyncDelegate(sp, cancellationToken);
                         await tenantBuilder.InitializeAsyncDelegate(sp, cancellationToken);
                     },
                    };

                    var tenantConfig = TenantConfig.BuildTenantAppConfiguration(config, hostingEnvironment, kvp.Key, options.ConfigureAppConfigurationDelegate);

                    tenantServices.AddSingleton(tenantConfig);

                    var builderContext = new TenantBuilderContext()
                    {
                        Configuration = tenantConfig,
                        HostingEnvironment = hostingEnvironment
                    };

                    options.ConfigureServicesDelegate(builderContext, tenantServices);

                    actionBuilder.Add(b => b.Populate(tenantServices));
                    mtc.ConfigureTenant(kvp.Key, actionBuilder.Build());
                }
            }
        }
    }

    public class AutofacOptions
    {
        internal Action<ContainerBuilder> ConfigureContainerDelegate { get; set; } = (builder => { });

        public AutofacOptions ConfigureContainer(Action<ContainerBuilder> configureContainerDelegate)
        {
            ConfigureContainerDelegate = configureContainerDelegate;
            return this;
        }
    }
    public class AutofacMultitenantOptions
    {
        internal Func<IServiceProvider, ITenantIdentificationStrategy> TenantIdentificationStrategyDelegate { get; set; } = (sp) => sp.GetService<ITenantIdentificationStrategy>() ?? new CompositeTenantIdentificationStrategy(sp.GetRequiredService<IHttpContextAccessor>(), new ITenantIdentificationStrategy[] { new DefaultQueryStringTenantIdentificationStrategy(sp.GetRequiredService<IHttpContextAccessor>(), sp.GetRequiredService<ILogger<DefaultQueryStringTenantIdentificationStrategy>>()), new DefaultSubdomainTenantIdentificationStrategy(sp.GetRequiredService<IHttpContextAccessor>(), sp.GetRequiredService<ILogger<DefaultSubdomainTenantIdentificationStrategy>>()) });

        public AutofacMultitenantOptions TenantIdentificationStrategy(Func<IServiceProvider, ITenantIdentificationStrategy> tenantIdentificationStrategyDelegate)
        {
            TenantIdentificationStrategyDelegate = tenantIdentificationStrategyDelegate;
            return this;
        }

        internal Action<ContainerBuilder> ConfigureContainerDelegate { get; set; } = (builder => { });

        public AutofacMultitenantOptions ConfigureContainer(Action<ContainerBuilder> configureContainerDelegate)
        {
            ConfigureContainerDelegate = configureContainerDelegate;
            return this;
        }

        internal Func<IContainer, ITenantIdentificationStrategy, MultitenantContainer> CreateMultiTenantContainerDelegate { get; set; } = (container, strategy) => {
            return new MultitenantContainer(strategy, container);
        };

        public AutofacMultitenantOptions CreateMultiTenantContainer(Func<IContainer, ITenantIdentificationStrategy, MultitenantContainer> createMultiTenantContainerDelegate)
        {
            CreateMultiTenantContainerDelegate = createMultiTenantContainerDelegate;
            return this;
        }
        public Dictionary<string, Action<TenantBuilder>> Tenants { get; set; } = new Dictionary<string, Action<TenantBuilder>>();

        public AutofacMultitenantOptions AddTenant(string tenantId)
        {
            Tenants.Add(tenantId, null);
            return this;
        }

        public AutofacMultitenantOptions AddTenant(string tenantId, Action<TenantBuilder> builder)
        {
            Tenants.Add(tenantId, builder);
            return this;
        }

        internal Action<TenantBuilder> ConfigureTenantsDelegate { get; set; } = (builder) => { };

        public AutofacMultitenantOptions ConfigureTenants(Action<TenantBuilder> configureTenantsDelegate)
        {
            ConfigureTenantsDelegate = configureTenantsDelegate;
            return this;
        }
    }

    public class TenantBuilder
    {
        internal Action<TenantBuilderContext, IConfigurationBuilder> ConfigureAppConfigurationDelegate { get; set; } = (context, builder) => { };

        public TenantBuilder ConfigureAppConfiguration(Action<TenantBuilderContext, IConfigurationBuilder> configureAppConfigurationDelegate)
        {
            ConfigureAppConfigurationDelegate = configureAppConfigurationDelegate;
            return this;
        }

        internal Action<TenantBuilderContext, IServiceCollection> ConfigureServicesDelegate { get; set; } = (context, services) => {};
        public TenantBuilder ConfigureServices(Action<TenantBuilderContext, IServiceCollection> configureServicesDelegate)
        {
            ConfigureServicesDelegate = configureServicesDelegate;
            return this;
        }

        internal Func<IServiceProvider, CancellationToken, Task> InitializeDbAsyncDelegate { get; set; } = (sp, cancellationToken) => Task.CompletedTask;

        public TenantBuilder InitializeDbAsync(Func<IServiceProvider, CancellationToken, Task> initializeDbAsyncDelegate)
        {
            InitializeDbAsyncDelegate = initializeDbAsyncDelegate;
            return this;
        }


        internal Func<IServiceProvider, CancellationToken, Task> InitializeAsyncDelegate { get; set; } = (sp, cancellationToken) => Task.CompletedTask;

        public TenantBuilder InitializeAsync(Func<IServiceProvider, CancellationToken, Task> initializeAsyncDelegate)
        {
            InitializeAsyncDelegate = initializeAsyncDelegate;
            return this;
        }
    }
}
