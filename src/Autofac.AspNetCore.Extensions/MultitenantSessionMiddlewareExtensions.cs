using Autofac.AspNetCore.Extensions.Middleware;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Text;

namespace Autofac.AspNetCore.Extensions
{
    public static class MultitenantSessionMiddlewareExtensions
    {
        public static IApplicationBuilder UseMultitenantSession(this IApplicationBuilder app)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            return app.UseMiddleware<MultitenantSessionMiddleware>();
        }

        public static IApplicationBuilder UseMultitenantSession(this IApplicationBuilder app, SessionOptions options)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            return app.UseMiddleware<MultitenantSessionMiddleware>(Options.Create(options));
        }
    }
}
