using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using System.Threading;
using System.Threading.Tasks;

namespace Autofac.AspNetCore.Extensions.Data
{
    public class DbContextTenantBase : DbContext
    {
        public ITenantService TenantService { get; }
        public DbContextTenantBase(DbContextOptions options, ITenantService tenantService)
            : base(options)
        {
            TenantService = tenantService;
        }

        public DbContextTenantBase(ITenantService tenantService)
        {
            TenantService = tenantService;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);

            TenantService?.GetTenantStrategy(this)?.OnConfiguring(optionsBuilder, TenantService.CurrentTenant, nameof(IEntityTenantFilter.TenantId));
            optionsBuilder.ReplaceService<IModelCacheKeyFactory, TenantModelCacheKeyFactory>();
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            TenantService?.GetTenantStrategy(this)?.OnModelCreating(modelBuilder, this, TenantService.CurrentTenant, nameof(IEntityTenantFilter.TenantId));
        }

        #region Save Changes
        public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default(CancellationToken))
        {
            TenantService?.GetTenantStrategy(this)?.OnSaveChanges(this, TenantService.CurrentTenant, nameof(IEntityTenantFilter.TenantId));

            return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }

        public override int SaveChanges()
        {

            TenantService?.GetTenantStrategy(this)?.OnSaveChanges(this, TenantService.CurrentTenant, nameof(IEntityTenantFilter.TenantId));

            return base.SaveChanges();
        }

        public override int SaveChanges(bool acceptAllChangesOnSuccess)
        {

            TenantService?.GetTenantStrategy(this)?.OnSaveChanges(this, TenantService.CurrentTenant, nameof(IEntityTenantFilter.TenantId));

            return base.SaveChanges(acceptAllChangesOnSuccess);
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken
            = default(CancellationToken))
        {

            TenantService?.GetTenantStrategy(this)?.OnSaveChanges(this, TenantService.CurrentTenant, nameof(IEntityTenantFilter.TenantId));

            return base.SaveChangesAsync(cancellationToken);
        }
        #endregion
    }
}
