using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using System.Threading;
using System.Threading.Tasks;

namespace Autofac.AspNetCore.Extensions
{
    public static class MultitenantInitializationHostExtensions
    {
        public static Task RunMultitenantInitializationAsync(this IWebHost host, CancellationToken cancellationToken = default)
        {
            return host.Services.RunMultitenantInitializationAsync(cancellationToken);
        }

        public static Task RunMultitenantInitializationAsync(this IHost host, CancellationToken cancellationToken = default)
        {
            return host.Services.RunMultitenantInitializationAsync(cancellationToken);
        }
    }
}
