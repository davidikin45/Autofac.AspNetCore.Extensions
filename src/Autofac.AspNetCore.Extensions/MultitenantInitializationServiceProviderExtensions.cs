using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Autofac.AspNetCore.Extensions
{
    public static class MultitenantInitializationServiceProviderExtensions
    {
        public static Task RunMultitenantInitializationAsync(this IServiceProvider services, CancellationToken cancellationToken)
        {
            var executor = services.GetRequiredService<MultitenantInitializationExecutor>();
            return executor.InitializeAsync(cancellationToken);
        }
    }
}
