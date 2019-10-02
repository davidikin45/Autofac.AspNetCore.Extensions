using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Autofac.AspNetCore.Extensions
{
    public static class TenantInitializationServiceProviderExtensions
    {
        public static Task RunTenantInitializationAsync(this IServiceProvider services, CancellationToken cancellationToken)
        {
            var executor = services.GetRequiredService<TenantInitializationExecutor>();
            return executor.InitializeAsync(cancellationToken);
        }
    }
}
