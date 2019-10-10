using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using EntityFrameworkCore.Initialization;

namespace Autofac.AspNetCore.Extensions.Data.IdentificationStrategies
{
    public class DifferentConnectionTenantDbContext<TDbContext> : IDbContextTenantStrategy<TDbContext>
        where TDbContext : DbContext
    {
        public string ConnectionStringName { get; }

        public DifferentConnectionTenantDbContext(string connectionStringName)
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
          
        }

        public void OnSaveChanges(DbContext context, ITenant tenant, string tenantPropertyName)
        {

        }
    }
}
