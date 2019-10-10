using Autofac.AspNetCore.Extensions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Threading.Tasks;
using WebApplication35.Data;

namespace WebApplication35
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
           var host = CreateHostBuilder(args).Build();

            using (var scope = host.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                await context.Database.EnsureCreatedAsync();
            }

            host.Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
             .UseAutofacMultitenant((context, options) =>
             {
                 options.MapDefaultTenantToAllRootDomains();
                 options.AddTenantsFromConfig(context.Configuration);
                 options.ConfigureTenants(builder =>
                 {
                     builder.MapToTenantIdSubDomain();
                     builder.InitializeAsync(async (serviceProvider, cancellationToken) =>
                     {
                         using (var scope = serviceProvider.CreateScope())
                         {
                             var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                             await context.Database.EnsureCreatedAsync();
                         }
                     });
                 });
             })
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.UseStartup<Startup>();
            });
    }
}
