using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using System;

namespace Autofac.AspNetCore.Extensions.Middleware
{
    internal class MultitenantRequestServicesStartupFilter : IStartupFilter
    {
        /// <summary>
        /// Adds the multitenant request services middleware to the app pipeline.
        /// </summary>
        /// <param name="next">
        /// The next middleware registration method that should execute.
        /// </param>
        /// <returns>
        /// The <see cref="Action{T}"/> for continued configuration or execution.
        /// </returns>
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
        {
            return builder =>
            {
                builder.UseMiddleware<MultitenantRequestServicesMiddleware>();
                next(builder);
            };
        }
    }
}
