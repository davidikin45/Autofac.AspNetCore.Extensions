namespace Autofac.AspNetCore.Extensions.Data
{
    public interface IDbContextTenantBase
    {
        ITenantService TenantService { get; }
    }
}
