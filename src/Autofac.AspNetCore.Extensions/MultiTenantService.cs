using Autofac.AspNetCore.Extensions.Data;
using Microsoft.EntityFrameworkCore;

namespace Autofac.AspNetCore.Extensions
{
    public class MultiTenantService : ITenantService
    {
        private readonly ITenantDbContextStrategyService _strategyService;
        public MultiTenantService(ITenantDbContextStrategyService strategyService, ITenant tenant)
        {
            _strategyService = strategyService;
            CurrentTenant = tenant;
        }

        public ITenant CurrentTenant { get; }

        public IDbContextTenantStrategy GetTenantStrategy(DbContext context)
        {
            return _strategyService.GetStrategy(context);
        }
    }
}
