using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Autofac.AspNetCore.Extensions.Data
{
    public class TenantModelCacheKeyFactory : IModelCacheKeyFactory
    {
        public object Create(DbContext context)
        {
            if (context is IDbContextTenantBase dynamicContext)
            {
                var tenantService = dynamicContext.TenantService;
                var tenantId = tenantService.CurrentTenant.Id;
                if (tenantId != null)
                {
                    return new { tenantId };
                }
            }

            return new ModelCacheKey(context);
        }
    }
}
