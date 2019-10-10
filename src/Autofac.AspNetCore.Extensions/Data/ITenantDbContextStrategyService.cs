using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace Autofac.AspNetCore.Extensions.Data
{
    public interface ITenantDbContextStrategyService
    {
        IDbContextTenantStrategy GetStrategy(DbContext context);
    }
}
