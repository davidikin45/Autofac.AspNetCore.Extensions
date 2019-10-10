using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace Autofac.AspNetCore.Extensions.Data
{
    public class MultiTenantDbContextStrategyService : ITenantDbContextStrategyService
    {
        private readonly IServiceProvider _serviceProvider;
        public MultiTenantDbContextStrategyService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public IDbContextTenantStrategy GetStrategy(DbContext context)
        {
            var dbContextType = context.GetType();
            var dbContextStrategyType = typeof(IDbContextTenantStrategy<>).MakeGenericType(dbContextType);
            return (IDbContextTenantStrategy)_serviceProvider.GetService(dbContextStrategyType);
        }

    }
}
