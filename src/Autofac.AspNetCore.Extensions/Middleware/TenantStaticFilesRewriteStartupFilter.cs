using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using System;
using System.Collections.Generic;
using System.Text;

namespace Autofac.AspNetCore.Extensions.Middleware
{
    //Addition to AppStartup.Configure for configuring Request Pipeline
    //https://andrewlock.net/exploring-istartupfilter-in-asp-net-core/
    public class TenantStaticFilesRewriteStartupFilter : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
        {
            return builder =>
            {
                //Adds the TenantMiddleware before ALL other middleware configured in Startup Configure.
                builder.UseMiddleware<MultitenantStaticFilesRewriteMiddleware>();
                next(builder);
            };
        }
    }
}
