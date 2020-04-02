using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Autofac.AspNetCore.Extensions.Security
{
    public class CookieAuthenticationMultitenantPostConfigureOptions : IPostConfigureOptions<CookieAuthenticationOptions>
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        public CookieAuthenticationMulitenantPostConfigureOptions(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public void PostConfigure(string name, CookieAuthenticationOptions options)
        {
            options.Events.OnSigningIn = OnSigningIn(options.Events.OnSigningIn);
            options.Events.OnValidatePrincipal = OnValidatePrincipal(options.Events.OnValidatePrincipal);

            var tenantId = _httpContextAccessor.HttpContext.GetTenantId() ?? string.Empty;

            if (options.Cookie.Name == null)
            {
                options.Cookie.Name = $"{tenantId}{CookieAuthenticationDefaults.CookiePrefix}{name}";
            }
            else
            {
                options.Cookie.Name = $"{tenantId}{options.Cookie.Name}";
            }
        }

        private static Func<CookieSigningInContext, Task> OnSigningIn(Func<CookieSigningInContext, Task> func)
        {
            return async (context) =>
            {
                ClaimsIdentity identity = (ClaimsIdentity)context.Principal.Identity;
                identity.AddClaim(new Claim("CookieName", context.Options.Cookie.Name));
                await func(context);
            };
        }

        private static Func<CookieValidatePrincipalContext, Task> OnValidatePrincipal(Func<CookieValidatePrincipalContext, Task> func)
        {
            return async (context) =>
            {
                ClaimsIdentity identity = (ClaimsIdentity)context.Principal.Identity;
                if (identity.FindFirst("CookieName").Value != context.Options.Cookie.Name)
                {
                    context.RejectPrincipal();
                }
            };
        }
    }
}