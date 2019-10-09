using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Options;

namespace Autofac.AspNetCore.Extensions.Security
{
    public class CookieTempDataPostConfigureOptions : IPostConfigureOptions<CookieTempDataProviderOptions>
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CookieTempDataPostConfigureOptions(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public void PostConfigure(string name, CookieTempDataProviderOptions options)
        {
            var tenantId = _httpContextAccessor.HttpContext.GetTenantId() ?? string.Empty;

            if (options.Cookie.Name == null)
            {
                options.Cookie.Name = $"{tenantId}{CookieTempDataProvider.CookieName}";
            }
            else
            {
                options.Cookie.Name = $"{tenantId}{options.Cookie.Name}";
            }
        }
    }
}
