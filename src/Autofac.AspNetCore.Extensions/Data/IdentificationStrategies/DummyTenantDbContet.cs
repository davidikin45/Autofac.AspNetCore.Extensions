using Microsoft.EntityFrameworkCore;

namespace Autofac.AspNetCore.Extensions.Data.IdentificationStrategies
{
    public class DummyTenantDbContext<TDbContext> : IDbContextTenantStrategy<TDbContext>
        where TDbContext : DbContext
    {
        public string ConnectionStringName => null;

        public void OnConfiguring(DbContextOptionsBuilder optionsBuilder, ITenant tenant, string tenantPropertyName)
        {
 
        }


        public void OnModelCreating(ModelBuilder modelBuilder, DbContext context, ITenant tenant, string tenantPropertyName)
        {

        }

        public void OnSaveChanges(DbContext context, ITenant tenant, string tenantPropertyName)
        { 

        }
    }
}
