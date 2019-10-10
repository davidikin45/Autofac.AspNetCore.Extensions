using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace Autofac.AspNetCore.Extensions.Data.Helpers
{
    public static class DbContextMultiTenancySaveExtensions
    {
        public static TDbContext SetTenantIds<TDbContext>(this TDbContext context, string tenantId, string tenantPropertyName = "TenantId") where TDbContext : DbContext
        {
            var added = context.ChangeTracker.Entries().Where(e => e.State == EntityState.Added);

            foreach (var entityEntry in added)
            {
                var property = entityEntry.Metadata.FindProperty(tenantPropertyName);
                if (property != null)
                {
                    entityEntry.Property(tenantPropertyName).CurrentValue = tenantId;
                }
            }

            return context;
        }
    }
}
