using Autofac.AspNetCore.Extensions.Middleware;
using Autofac.Extensions.DependencyInjection;
using Autofac.Integration.AspNetCore.Multitenant;
using Autofac.Multitenant;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
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

            if (options.ConfigureStaticFilesDelegate != null)
            {
                services.ConfigureMultitenantStaticFilesRewriteOptions(options.ConfigureStaticFilesDelegate);
            }

            services.AddSingleton<IStartupFilter, Tenant404StartupFilter>();

            services.AddSingleton<IStartupFilter, TenantStaticFilesRewriteStartupFilter>();

            services.AddTransient<MultitenantInitializationExecutor>();
            services.AddSingleton(options);

            return services.AddSingleton<IServiceProviderFactory<ContainerBuilder>>(sp => new AutofacMultiTenantServiceProviderFactory(sp.GetRequiredService<AutofacMultitenantOptions>()));
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

                mtc = _options.CreateMultiTenantContainerDelegate(container, strategy);

                ConfigureTenants(mtc);

                var provider = new AutofacMultitenantServiceProvider(mtc);

                return provider;
            }

            private void ConfigureTenants(MultitenantContainer mtc)
            {
                var serviceProvider = new AutofacServiceProvider(mtc);

                var hostingEnvironment = serviceProvider.GetRequiredService<Microsoft.AspNetCore.Hosting.IHostingEnvironment>();
                var config = serviceProvider.GetRequiredService<IConfiguration>();
                var tenantConfigurations = serviceProvider.GetServices<ITenantConfiguration>();

                if (_options.AutoAddITenantConfigurationTenants)
                {
                    foreach (var tenantId in tenantConfigurations.Select(i => i.TenantId.ToString()))
                    {
                        if (_options.Tenants.ContainsKey(tenantId))
                        {
                            _options.Tenants.Add(tenantId, null);
                        }
                    }
                }

                foreach (var kvp in _options.Tenants)
                {
                    var tenantConfiguration = tenantConfigurations.FirstOrDefault(i => i.TenantId.ToString() == kvp.Key);

                    var actionBuilder = new ConfigurationActionBuilder();
                    var tenantServices = new ServiceCollection();

                    var defaultBuilder = new TenantBuilder();

                    foreach (var ConfigureTenantsDelegate in _options.ConfigureTenantsDelegates)
                    {
                        ConfigureTenantsDelegate(defaultBuilder);
                    }

                    var tenantBuilder = new TenantBuilder();
                    if (kvp.Value != null)
                        kvp.Value(tenantBuilder);

                    var options = new TenantBuilder()
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
                        InitializeDbAsyncDelegate = async (sp, cancellationToken) =>
                     {
                         await defaultBuilder.InitializeDbAsyncDelegate(sp, cancellationToken);
                         await tenantBuilder.InitializeDbAsyncDelegate(sp, cancellationToken);
                         if (tenantConfiguration != null)
                             await tenantConfiguration.InitializeDbAsync(sp, cancellationToken);
                     },
                        InitializeAsyncDelegate = async (sp, cancellationToken) =>
                     {
                         await defaultBuilder.InitializeAsyncDelegate(sp, cancellationToken);
                         await tenantBuilder.InitializeAsyncDelegate(sp, cancellationToken);
                         if (tenantConfiguration != null)
                             await tenantConfiguration.InitializeAsync(sp, cancellationToken);
                     },
                    };

                    var tenantConfig = TenantConfig.BuildTenantAppConfiguration(config, hostingEnvironment, kvp.Key, options.ConfigureAppConfigurationDelegate);

                    tenantServices.AddSingleton(tenantConfig);

                    var builderContext = new TenantBuilderContext(kvp.Key)
                    {
                        RootServiceProvider = serviceProvider,
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
        public bool AutoAddITenantConfigurationTenants { get; set; } = true;
        internal Func<IServiceProvider, ITenantIdentificationStrategy> TenantIdentificationStrategyDelegate { get; set; } = (sp) => sp.GetService<ITenantIdentificationStrategy>() ?? new CompositeTenantIdentificationStrategy(sp.GetRequiredService<IHttpContextAccessor>(), new ITenantIdentificationStrategy[] { 
            new DefaultQueryStringTenantIdentificationStrategy(sp.GetRequiredService<IHttpContextAccessor>(), sp.GetRequiredService<ILogger<DefaultQueryStringTenantIdentificationStrategy>>(), sp.GetRequiredService<AutofacMultitenantOptions>()), 
            new DefaultSubdomainTenantIdentificationStrategy(sp.GetRequiredService<IHttpContextAccessor>(), sp.GetRequiredService<ILogger<DefaultSubdomainTenantIdentificationStrategy>>(), sp.GetRequiredService<AutofacMultitenantOptions>()) });

        public AutofacMultitenantOptions TenantIdentificationStrategy(Func<IServiceProvider, ITenantIdentificationStrategy> tenantIdentificationStrategyDelegate)
        {
            TenantIdentificationStrategyDelegate = tenantIdentificationStrategyDelegate;
            return this;
        }

        internal Action<MultitenantStaticFilesRewriteOptions> ConfigureStaticFilesDelegate { get; set; }

        public AutofacMultitenantOptions ConfigureStaticFiles(Action<MultitenantStaticFilesRewriteOptions> configureStaticFilesDelegate)
        {
            ConfigureStaticFilesDelegate = configureStaticFilesDelegate;
            return this;
        }

        internal Action<ContainerBuilder> ConfigureContainerDelegate { get; set; } = (builder => { });

        public AutofacMultitenantOptions ConfigureContainer(Action<ContainerBuilder> configureContainerDelegate)
        {
            ConfigureContainerDelegate = configureContainerDelegate;
            return this;
        }

        internal Func<IContainer, ITenantIdentificationStrategy, MultitenantContainer> CreateMultiTenantContainerDelegate { get; set; } = (container, strategy) =>
        {
            return new MultitenantContainer(strategy, container);
        };

        public AutofacMultitenantOptions CreateMultiTenantContainer(Func<IContainer, ITenantIdentificationStrategy, MultitenantContainer> createMultiTenantContainerDelegate)
        {
            CreateMultiTenantContainerDelegate = createMultiTenantContainerDelegate;
            return this;
        }
        public Dictionary<string, Action<TenantBuilder>> Tenants { get; set; } = new Dictionary<string, Action<TenantBuilder>>() { { "", null } };

        public AutofacMultitenantOptions AddTenant(string tenantId)
        {
            Tenants.Add(tenantId, null);
            return this;
        }

        public AutofacMultitenantOptions AddTenants(IEnumerable<string> tenantIds, Action<TenantBuilder> builder)
        {
            foreach (var tenantId in tenantIds)
            {
                Tenants.Add(tenantId, builder);
            }
            return this;
        }

        public AutofacMultitenantOptions AddTenants(IEnumerable<string> tenantIds)
        {
            foreach (var tenantId in tenantIds)
            {
                Tenants.Add(tenantId, null);
            }

            return this;
        }

        public AutofacMultitenantOptions RemoveDefaultTenant()
        {
            return RemoveTenants(string.Empty);
        }

        public AutofacMultitenantOptions AddTenantsFromConfig(IConfiguration config)
        {
            return AddTenants(config.GetTenants());
        }

        public AutofacMultitenantOptions AddTenants(params string[] tenantIds)
        {
            foreach (var tenantId in tenantIds)
            {
                Tenants.Add(tenantId, null);
            }
            return this;
        }

        public AutofacMultitenantOptions RemoveTenants(params string[] tenantIds)
        {
            foreach (var tenantId in tenantIds)
            {
                Tenants.Remove(tenantId);
            }
            return this;
        }

        public AutofacMultitenantOptions AddTenants(string tenantId, Action<TenantBuilder> builder)
        {
            Tenants.Add(tenantId, builder);
            return this;
        }

        internal List<Action<TenantBuilder>> ConfigureTenantsDelegates { get; set; } = new List<Action<TenantBuilder>>(){ (builder) => {

            //https://www.finbuckle.com/MultiTenant/Docs/Authentication
            builder.ConfigureServices((context, services) =>
            {
                services.AddTransient<IConfigureOptions<RazorPagesOptions>, RazorPagesOptionsSetup>();
            });
        }};

        public AutofacMultitenantOptions ConfigureTenants(Action<TenantBuilder> configureTenantsDelegate)
        {
            ConfigureTenantsDelegates.Add(configureTenantsDelegate);
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

        internal Action<TenantBuilderContext, IServiceCollection> ConfigureServicesDelegate { get; set; } = (context, services) => { };
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

        public object TenantId { get; }

        public TenantBuilder InitializeAsync(Func<IServiceProvider, CancellationToken, Task> initializeAsyncDelegate)
        {
            InitializeAsyncDelegate = initializeAsyncDelegate;
            return this;
        }
    }
}
