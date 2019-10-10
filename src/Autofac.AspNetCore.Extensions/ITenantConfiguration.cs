using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Autofac.AspNetCore.Extensions
{
    public interface ITenantConfiguration
    {
        object TenantId { get; }
        IEnumerable<string> HostNames { get;  }

        void ConfigureAppConfiguration(TenantBuilderContext context, IConfigurationBuilder builder);

        void ConfigureServices(TenantBuilderContext context, IServiceCollection services);

        Task InitializeDbAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken);

        Task InitializeAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken);
    }
}
