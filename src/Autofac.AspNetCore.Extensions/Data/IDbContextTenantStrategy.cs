using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Autofac.AspNetCore.Extensions.Data
{
    public interface IDbContextTenantStrategy<TDbContext> : IDbContextTenantStrategy
        where TDbContext : DbContext
    {

    }
    public interface IDbContextTenantStrategy
    {
        string ConnectionStringName { get; }
        void OnConfiguring(DbContextOptionsBuilder optionsBuilder, ITenant tenant, string tenantPropertyName);
        void OnModelCreating(ModelBuilder modelBuilder, DbContext context, ITenant tenant, string tenantPropertyName);
        void OnSaveChanges(DbContext tenantDbContext, ITenant tenant, string tenantPropertyName);
    }
}
