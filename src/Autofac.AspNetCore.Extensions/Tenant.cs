using Microsoft.Extensions.Configuration;

namespace Autofac.AspNetCore.Extensions
{
    public class Tenant : ITenant
    {
        public string Id { get; }

        public IConfiguration Configuration { get; }

        public Tenant(string tenantId, IConfiguration configuration)
        {
            Id = tenantId;
            Configuration = configuration;
        }
    }
}
