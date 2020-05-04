using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System;

namespace Autofac.AspNetCore.Extensions
{
    public class TenantBuilderContext
    {
        public TenantBuilderContext(string tenantId)
        {
            TenantId = tenantId;
        }

        public string TenantId { get; }
        public IServiceProvider RootServiceProvider { get; set; }
        public IConfiguration Configuration { get; set; }
        public IHostEnvironment HostingEnvironment { get; set; }
    }
}
