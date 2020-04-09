using Autofac.AspNetCore.Extensions.Data;
using Autofac.AspNetCore.Extensions.Middleware;
using Autofac.Extensions.DependencyInjection;
using Autofac.Integration.AspNetCore.Multitenant;
using Autofac.Multitenant;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
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
                if(_options.ValidateOnBuild)
                {
                    services.BuildServiceProvider(new ServiceProviderOptions() { ValidateOnBuild = true });
                }

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

            foreach (var configureServices in options.ConfigureServicesList)
            {
                configureServices(services);
            }

            if (options.ConfigureStaticFilesDelegate != null)
            {
                services.ConfigureMultitenantStaticFilesRewriteOptions(options.ConfigureStaticFilesDelegate);
            }

            services.AddSingleton<IStartupFilter, Tenant404StartupFilter>();

            //services.AddSingleton<IStartupFilter, TenantStaticFilesRewriteStartupFilter>();

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
                if (_options.ValidateOnBuild)
                {
                    services.BuildServiceProvider(new ServiceProviderOptions() { ValidateOnBuild = true });
                }

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

                    var defaultBuilder = new TenantBuilder(kvp.Key);

                    foreach (var ConfigureTenantsDelegate in _options.ConfigureTenantsDelegates)
                    {
                        ConfigureTenantsDelegate(defaultBuilder);
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
                         }
                    };

                    foreach (var hostName in defaultBuilder.HostNames)
                    {
                        _options.HostMappings.Add(hostName, kvp.Key);
                    }

                    foreach (var hostName in tenantBuilder.HostNames)
                    {
                        _options.HostMappings.Add(hostName, kvp.Key);
                    }

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

        public static TenantDbContextIdentification<TContext> AddDbContextStrategy<TContext>(this IServiceCollection services)
        where TContext : DbContext, IDbContextTenantBase
        {
            return new TenantDbContextIdentification<TContext>(services);
        }
    }

    public class AutofacOptions
    {
        public bool ValidateOnBuild { get; set; } = false;
        internal Action<ContainerBuilder> ConfigureContainerDelegate { get; set; } = (builder => { });

        public AutofacOptions ConfigureContainer(Action<ContainerBuilder> configureContainerDelegate)
        {
            ConfigureContainerDelegate = configureContainerDelegate;
            return this;
        }
    }
    public class AutofacMultitenantOptions
    {
        public bool ValidateOnBuild { get; set; } = false;
        public bool AllowDefaultTenantRequests { get; set; } = true;
        public bool AutoAddITenantConfigurationTenants { get; set; } = true;

        public Dictionary<string, string> SubdomainMappings = new Dictionary<string, string>();

        public Dictionary<string, string> HostMappings = new Dictionary<string, string>();
        internal Func<IServiceProvider, ITenantIdentificationStrategy> TenantIdentificationStrategyDelegate { get; set; } = (sp) => sp.GetService<ITenantIdentificationStrategy>() ?? new CompositeTenantIdentificationStrategy(sp.GetRequiredService<IHttpContextAccessor>(), new ITenantIdentificationStrategy[] {
            new DefaultQueryStringTenantIdentificationStrategy(sp.GetRequiredService<IHttpContextAccessor>(), sp.GetRequiredService<ILogger<DefaultQueryStringTenantIdentificationStrategy>>(), sp.GetRequiredService<AutofacMultitenantOptions>()),
            new DefaultHostTenantIdentificationStrategy(sp.GetRequiredService<IHttpContextAccessor>(), sp.GetRequiredService<ILogger<DefaultHostTenantIdentificationStrategy>>(), sp.GetRequiredService<AutofacMultitenantOptions>())
        });

        public AutofacMultitenantOptions TenantIdentificationStrategy(Func<IServiceProvider, ITenantIdentificationStrategy> tenantIdentificationStrategyDelegate)
        {
            TenantIdentificationStrategyDelegate = tenantIdentificationStrategyDelegate;
            return this;
        }

        internal List<Action<IServiceCollection>> ConfigureServicesList = new List<Action<IServiceCollection>>();

        public AutofacMultitenantOptions ConfigureServices(Action<IServiceCollection> configureServicesDelegate)
        {
            ConfigureServicesList.Add(configureServicesDelegate);
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

        public AutofacMultitenantOptions MapDefaultTenantToHost(params string[] hosts)
        {
            foreach (var host in hosts)
            {
                HostMappings.Add(host, null);
            }
            return this;
        }

        public AutofacMultitenantOptions MapDefaultTenantToAllRootDomains()
        {
            HostMappings.Add("", null);
            return this;
        }

        public AutofacMultitenantOptions MapDefaultTenantToAllRootAndSubDomains()
        {
            HostMappings.Add("*", null);
            return this;
        }

        public AutofacMultitenantOptions MapDefaultTenantToSubDomain(params string[] subdomains)
        {
            foreach (var subdomain in subdomains)
            {
                HostMappings.Add($"{subdomain}.*", null);
            }
            return this;
        }

        public AutofacMultitenantOptions MapTenantToSubDomain(string tenantId, params string[] subdomains)
        {
            foreach (var subdomain in subdomains)
            {
                HostMappings.Add($"{subdomain}.*", tenantId);
            }
            return this;
        }
        public AutofacMultitenantOptions MapTenantToHost(string tenantId, params string[] hosts)
        {
            foreach (var host in hosts)
            {
                HostMappings.Add(host, tenantId);
            }
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
        public Dictionary<string, Action<TenantBuilder>> Tenants { get; set; } = new Dictionary<string, Action<TenantBuilder>>();

        public AutofacMultitenantOptions AddTenant(string tenantId)
        {
            AddTenant(tenantId, null);
            return this;
        }

        public AutofacMultitenantOptions AddTenant(string tenantId, Action<TenantBuilder> builder)
        {
            Tenants.Add(tenantId, builder);
            //HostMappings.Add($"{tenantId}.*", tenantId);
            return this;
        }

        public AutofacMultitenantOptions AddTenants(IEnumerable<string> tenantIds, Action<TenantBuilder> builder)
        {
            foreach (var tenantId in tenantIds)
            {
                AddTenant(tenantId, builder);
            }
            return this;
        }

        public AutofacMultitenantOptions AddTenants(IEnumerable<string> tenantIds)
        {
            foreach (var tenantId in tenantIds)
            {
                AddTenant(tenantId, null);
            }

            return this;
        }

        public AutofacMultitenantOptions AddTenantsFromConfig(IConfiguration config)
        {
            return AddTenants(config.GetTenants());
        }

        public AutofacMultitenantOptions AddTenants(params string[] tenantIds)
        {
            foreach (var tenantId in tenantIds)
            {
                AddTenant(tenantId, null);
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
            AddTenant(tenantId, builder);
            return this;
        }

        internal List<Action<TenantBuilder>> ConfigureTenantsDelegates { get; set; } = new List<Action<TenantBuilder>>(){ (builder) => {

            //https://www.finbuckle.com/MultiTenant/Docs/Authentication
            builder.ConfigureServices((context, services) =>
            {
                var tenant = new Tenant(context.TenantId, context.Configuration);
                services.AddSingleton<ITenant>(tenant);

                services.AddTransient<IPostConfigureOptions<RazorPagesOptions>, RazorPagesOptionsSetup>();
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
        public object TenantId { get; }
        public TenantBuilder(object tenantId)
        {
            TenantId = tenantId;
        }

        public HashSet<string> HostNames = new HashSet<string>();

        public TenantBuilder MapToTenantIdSubDomain()
        {
            return MapToSubDomain(TenantId.ToString());
        }

        public TenantBuilder MapToSubDomain(params string[] subdomains)
        {
            foreach (var subdomain in subdomains)
            {
                HostNames.Add($"{subdomain}.*");
            }
            return this;
        }

        public TenantBuilder MapToAllRootDomains()
        {
            HostNames.Add("");
            return this;
        }

        public TenantBuilder MapToHost(params string[] hosts)
        {
            foreach (var host in hosts)
            {
                HostNames.Add(host);
            }
            return this;
        }

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

        public TenantBuilder InitializeAsync(Func<IServiceProvider, CancellationToken, Task> initializeAsyncDelegate)
        {
            InitializeAsyncDelegate = initializeAsyncDelegate;
            return this;
        }
    }
}
