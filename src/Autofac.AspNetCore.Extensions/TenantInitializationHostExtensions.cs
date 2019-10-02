using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using System.Threading;
using System.Threading.Tasks;

namespace Autofac.AspNetCore.Extensions
{
    public static class TenantInitializationHostExtensions
    {
        public static Task RunTenantInitializationAsync(this IWebHost host, CancellationToken cancellationToken = default)
        {
            return host.Services.RunTenantInitializationAsync(cancellationToken);
        }

        public static Task RunTenantInitializationAsync(this IHost host, CancellationToken cancellationToken = default)
        {
            return host.Services.RunTenantInitializationAsync(cancellationToken);
        }
    }
}
