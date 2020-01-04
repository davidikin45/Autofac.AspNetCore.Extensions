using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using EntityFrameworkCore.Initialization;

namespace Autofac.AspNetCore.Extensions.Data.IdentificationStrategies
{
    public class DifferentSqliteDbContext<TDbContext> : IDbContextTenantStrategy<TDbContext>
        where TDbContext : DbContext
    {
        public string ConnectionStringName => null;

        public void OnConfiguring(DbContextOptionsBuilder optionsBuilder, ITenant tenant, string tenantPropertyName)
        {
            var connectionString = $"Data Source={tenant.Id}.db;";
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
