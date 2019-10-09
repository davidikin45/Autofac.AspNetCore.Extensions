using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
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
        public IHostingEnvironment HostingEnvironment { get; set; }
    }
}
