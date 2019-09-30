using Autofac.Extensions.DependencyInjection;
using Autofac.Integration.AspNetCore.Multitenant;
using Autofac.Multitenant;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;

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

                _options.ConfigureContainer(containerBuilder);


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

            return services.AddSingleton<IServiceProviderFactory<ContainerBuilder>>(new AutofacMultiTenantServiceProviderFactory(options));
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

                _options.ConfigureContainer(containerBuilder);

                return containerBuilder;
            }

            public IServiceProvider CreateServiceProvider(ContainerBuilder builder)
            {

                if (builder == null) throw new ArgumentNullException(nameof(builder));

                AutofacMultitenantServiceProvider provider = null;

                builder.Register(_ => provider)
               .As<IServiceProvider>()
               .ExternallyOwned();

                //IServiceProvider.GetAutofacMultitenantRoot()
                MultitenantContainer mtc = null;

                builder.Register(_ => mtc)
                .AsSelf()
                .ExternallyOwned();

                var container = builder.Build();

                var strategy = _options.TenantIdentificationStrategy(new AutofacServiceProvider(container));

                mtc = _options.CreateMultiTenantContainer(container, strategy);

                provider = new AutofacMultitenantServiceProvider(mtc);

                return provider;
            }
        }
    }
    public class AutofacOptions
    {
        public Action<ContainerBuilder> ConfigureContainer { get; set; } = (builder => { });
    }
    public class AutofacMultitenantOptions
    {
        public Func<IServiceProvider, ITenantIdentificationStrategy> TenantIdentificationStrategy { get; set; } = (sp) => sp.GetService<ITenantIdentificationStrategy>() ?? new DefaultQueryStringTenantIdentificationStrategy(sp.GetRequiredService<IHttpContextAccessor>(), sp.GetRequiredService<ILogger<DefaultQueryStringTenantIdentificationStrategy>>());
        public Action<ContainerBuilder> ConfigureContainer { get; set; } = (builder => { });

        public Func<IContainer, ITenantIdentificationStrategy, MultitenantContainer> CreateMultiTenantContainer { get; set; } = (container, strategy) => {
            return new MultitenantContainer(strategy, container);
        };
    }
}
