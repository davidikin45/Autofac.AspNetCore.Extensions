using Autofac.AspNetCore.Extensions.Data.Helpers;
using Microsoft.EntityFrameworkCore;

namespace Autofac.AspNetCore.Extensions.Data.IdentificationStrategies
{
    public sealed class FilterTenantDbContext<TDbContext> : IDbContextTenantStrategy<TDbContext>
        where TDbContext : DbContext
    {
        public string ConnectionStringName => null;

        public void OnConfiguring(DbContextOptionsBuilder optionsBuilder, ITenant tenant, string tenantPropertyName)
        {

        }

        public void OnModelCreating(ModelBuilder modelBuilder, DbContext context, ITenant tenant, string tenantPropertyName)
        {
            modelBuilder.AddTenantFilter(tenant.Id, tenantPropertyName);
            modelBuilder.AddTenantShadowPropertyFilter(tenant.Id, tenantPropertyName, true);
        }

        public void OnSaveChanges(DbContext context, ITenant tenant, string tenantPropertyName)
        {

            context.SetTenantIds(tenant.Id, tenantPropertyName);
        }
    }
}
