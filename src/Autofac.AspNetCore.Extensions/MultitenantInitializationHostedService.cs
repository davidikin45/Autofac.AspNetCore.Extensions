using Microsoft.Extensions.Hosting;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Autofac.AspNetCore.Extensions
{
    public class MultitenantInitializationHostedService : IHostedService
    {
        private readonly IServiceProvider _serviceProvider;
        public MultitenantInitializationHostedService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }
        public Task StartAsync(CancellationToken cancellationToken)
        {
            return _serviceProvider.RunMultitenantInitializationAsync(cancellationToken);
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
