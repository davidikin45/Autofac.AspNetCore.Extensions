using Autofac.AspNetCore.Extensions.Middleware;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Autofac.AspNetCore.Extensions
{
    public static class MultitenantStaticFileExtensions
    {
        public static IServiceCollection ConfigureMultitenantStaticFilesRewriteOptions(this IServiceCollection services, Action<MultitenantStaticFilesRewriteOptions> configure)
        {
            return services.Configure(configure);
        }

        public static IApplicationBuilder UseMulitenantStaticFiles(this IApplicationBuilder app)
        {
            return app.UseMiddleware<MultitenantStaticFilesRewriteMiddleware>();
        }
    }
}
