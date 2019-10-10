using Autofac.AspNetCore.Extensions.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace Autofac.AspNetCore.Extensions
{
    public interface ITenantService
    {
        IDbContextTenantStrategy GetTenantStrategy(DbContext context);
        ITenant CurrentTenant { get; }
    }
}
