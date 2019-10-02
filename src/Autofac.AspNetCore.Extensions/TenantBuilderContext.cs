using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace Autofac.AspNetCore.Extensions
{
    public class TenantBuilderContext
    {
        public IConfiguration Configuration { get; set; }
        public IHostingEnvironment HostingEnvironment { get; set; }
    }
}
