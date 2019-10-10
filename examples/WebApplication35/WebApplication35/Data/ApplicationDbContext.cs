using Autofac.AspNetCore.Extensions;
using Autofac.AspNetCore.Extensions.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace WebApplication35.Data
{
    public class ApplicationDbContext : DbContextIdentityTenantBase<IdentityUser>, IDbContextTenantBase
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, ITenantService tenantService)
            : base(options, tenantService)
        {

        }
    }
}
