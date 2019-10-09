using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Session;
using Microsoft.Extensions.Options;

namespace Autofac.AspNetCore.Extensions.Security
{
    public class SessionPostConfigureOptions : IPostConfigureOptions<SessionOptions>
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public SessionPostConfigureOptions(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public void PostConfigure(string name, SessionOptions options)
        {
            var tenantId = _httpContextAccessor.HttpContext.GetTenantId() ?? string.Empty;

            if (options.Cookie.Name == null)
            {
                options.Cookie.Name = $"{tenantId}{SessionDefaults.CookieName}";
            }
            else
            {
                options.Cookie.Name = $"{tenantId}{options.Cookie.Name}";
            }
        }
    }
}
