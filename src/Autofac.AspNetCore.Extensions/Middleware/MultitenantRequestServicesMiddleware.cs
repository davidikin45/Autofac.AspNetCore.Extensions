﻿using Autofac.Multitenant;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

namespace Autofac.AspNetCore.Extensions.Middleware
{
    internal class MultitenantRequestServicesMiddleware
    {
        private readonly IHttpContextAccessor _contextAccessor;

        private readonly RequestDelegate _next;

        private readonly IServiceProvider _serviceProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="MultitenantRequestServicesMiddleware"/> class.
        /// </summary>
        /// <param name="next">The next step in the request pipeline.</param>
        /// <param name="contextAccessor">The <see cref="IHttpContextAccessor"/> to set up with the current request context.</param>
        /// <param name="serviceProvider">The <see cref="IServiceProvider"/> to retrieve the <see cref="MultitenantContainer"/> registered through <see cref="AutofacMultitenantServiceProvider"/>.</param>
        public MultitenantRequestServicesMiddleware(RequestDelegate next, IHttpContextAccessor contextAccessor, IServiceProvider serviceProvider)
        {
            this._next = next;
            this._contextAccessor = contextAccessor;
            this._serviceProvider = serviceProvider;
        }

        /// <summary>
        /// Invokes the middleware using the specified context.
        /// </summary>
        /// <param name="context">
        /// The request context to process through the middleware.
        /// </param>
        /// <returns>
        /// A <see cref="Task"/> to await for completion of the operation.
        /// </returns>
        public async Task Invoke(HttpContext context)
        {
            // If there isn't already an HttpContext set on the context
            // accessor for this async/thread operation, set it. This allows
            // tenant identification to use it.
            if (this._contextAccessor.HttpContext == null)
            {
                this._contextAccessor.HttpContext = context;
            }

            // this throws an invalid-operation-exception, when IServiceProvider can't be casted down to ILifetimeScope or MultitenantContainer
            var container = this._serviceProvider.GetRequiredService<MultitenantContainer>();

            IServiceProvidersFeature existingFeature = null;
            try
            {
                var autofacFeature = RequestServicesFeatureFactory.CreateFeature(context, container.Resolve<IServiceScopeFactory>());

                if (autofacFeature is IDisposable disp)
                {
                    context.Response.RegisterForDispose(disp);
                }

                existingFeature = context.Features.Get<IServiceProvidersFeature>();
                context.Features.Set(autofacFeature);

                await this._next.Invoke(context);
            }
            finally
            {
                // In ASP.NET Core 1.x the existing feature will disposed as part of
                // a using statement; in ASP.NET Core 2.x it is registered directly
                // with the response for disposal. In either case, we don't have to
                // do that. We do put back any existing feature, though, since
                // at this point there may have been some default tenant or base
                // container level stuff resolved and after this middleware it needs
                // to be what it was before.
                context.Features.Set(existingFeature);
            }
        }
    }
}
