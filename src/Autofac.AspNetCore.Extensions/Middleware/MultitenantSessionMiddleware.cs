using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Session;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Autofac.AspNetCore.Extensions.Middleware
{
    public class MultitenantSessionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILoggerFactory _loggerFactory;
        private readonly ISessionStore _sessionStore;
        private readonly IDataProtectionProvider _dataProtectionProvider;
        public MultitenantSessionMiddleware(
           RequestDelegate next,
           ILoggerFactory loggerFactory,
           IDataProtectionProvider dataProtectionProvider,
           ISessionStore sessionStore,
           IOptions<SessionOptions> options)
        {
            if (next == null)
            {
                throw new ArgumentNullException(nameof(next));
            }

            if (loggerFactory == null)
            {
                throw new ArgumentNullException(nameof(loggerFactory));
            }

            if (dataProtectionProvider == null)
            {
                throw new ArgumentNullException(nameof(dataProtectionProvider));
            }

            if (sessionStore == null)
            {
                throw new ArgumentNullException(nameof(sessionStore));
            }

            _next = next;
            _loggerFactory = loggerFactory;
            _dataProtectionProvider = dataProtectionProvider;
            _sessionStore = sessionStore;
        }
        public Task Invoke(HttpContext context)
        {
            var options = context.RequestServices.GetRequiredService<IOptions<SessionOptions>>();
            var sessionMiddleware = new SessionMiddleware(_next, _loggerFactory, _dataProtectionProvider, _sessionStore, options);
            return sessionMiddleware.Invoke(context);
        }
    }
}
