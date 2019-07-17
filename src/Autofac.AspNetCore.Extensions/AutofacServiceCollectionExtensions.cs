using Autofac.Extensions.DependencyInjection;
using Autofac.Multitenant;
using Microsoft.Extensions.DependencyInjection;
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

        private class AutofacServiceProviderFactory : IServiceProviderFactory<ContainerBuilder>
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

                var container = builder.Build();

                return new AutofacServiceProvider(container);
            }
        }

        public class AutofacOptions
        {
            public Action<ContainerBuilder> ConfigureContainer { get; set; } = (builder => { });
        }

        public static IServiceCollection AddAutofacMultitenant(this IServiceCollection services, Action<MultitenantContainer> mtcSetter, Action<AutofacMultitenantOptions> setupAction = null)
        {
            var options = new AutofacMultitenantOptions();
            if (setupAction != null)
                setupAction(options);

            return services.AddSingleton<IServiceProviderFactory<ContainerBuilder>>(new AutofacMultiTenantServiceProviderFactory(mtcSetter, options));
        }
        private class AutofacMultiTenantServiceProviderFactory : IServiceProviderFactory<ContainerBuilder>
        {
            private Action<MultitenantContainer> _mtcSetter;
            private readonly AutofacMultitenantOptions _options;

            public AutofacMultiTenantServiceProviderFactory(Action<MultitenantContainer> mtcSetter, AutofacMultitenantOptions options)
            {
                _mtcSetter = mtcSetter;
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

                var container = builder.Build();

                var tenantIdentificationStrategy = _options.TenantIdentificationStrategy(new AutofacServiceProvider(container));

                var mtc = new MultitenantContainer(tenantIdentificationStrategy, container);

                _mtcSetter(mtc);

                return new AutofacServiceProvider(mtc);
            }
        }

        public class AutofacMultitenantOptions
        {
            public Func<IServiceProvider, ITenantIdentificationStrategy> TenantIdentificationStrategy { get; set; } = (sp) => sp.GetRequiredService<ITenantIdentificationStrategy>();
            public Action<ContainerBuilder> ConfigureContainer { get; set; } = (builder => { });
        }
    }
}
