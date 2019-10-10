using Autofac.AspNetCore.Extensions.Data.Helpers;
using EntityFrameworkCore.Initialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Autofac.AspNetCore.Extensions.Data.IdentificationStrategies
{
    public sealed class DifferentConnectionFilterTenantDifferentSchemaDbContext<TDbContext> : IDbContextTenantStrategy<TDbContext>
        where TDbContext : DbContext
    {
        public string ConnectionStringName { get; }

        public DifferentConnectionFilterTenantDifferentSchemaDbContext(string connectionStringName)
        {
            ConnectionStringName = connectionStringName;
        }

        public void OnConfiguring(DbContextOptionsBuilder optionsBuilder, ITenant tenant, string tenantPropertyName)
        {
            var connectionString = tenant.Configuration.GetConnectionString(ConnectionStringName);
            optionsBuilder.SetConnectionString<TDbContext>(connectionString);
        }

        public void OnModelCreating(ModelBuilder modelBuilder, DbContext context, ITenant tenant, string tenantPropertyName)
        {
            modelBuilder.AddTenantSchema(tenant.Id);
            modelBuilder.AddTenantFilter(tenant.Id, tenantPropertyName);
            modelBuilder.AddTenantShadowPropertyFilter(tenant.Id, tenantPropertyName, true);
        }

        public void OnSaveChanges(DbContext context, ITenant tenant, string tenantPropertyName)
        {
            context.SetTenantIds(tenant.Id, tenantPropertyName);
        }
    }
}
