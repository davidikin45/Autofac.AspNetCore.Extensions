using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Autofac.AspNetCore.Extensions.Security
{
    public class AntiForgeryMultitenantPostConfigureOptions : IPostConfigureOptions<AntiforgeryOptions>
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly DataProtectionOptions _dataProtectionOptions;

        public AntiForgeryMultitenantPostConfigureOptions(IHttpContextAccessor httpContextAccessor, IOptions<DataProtectionOptions> dataProtectionOptions)
        {
            _httpContextAccessor = httpContextAccessor;
            _dataProtectionOptions = dataProtectionOptions.Value;
        }

        public void PostConfigure(string name, AntiforgeryOptions options)
        {
            var tenantId = _httpContextAccessor.HttpContext.GetTenantId() ?? string.Empty;

            if (options.Cookie.Name == null)
            {
                var applicationId = _dataProtectionOptions.ApplicationDiscriminator ?? string.Empty;
                options.Cookie.Name = $"{tenantId}{CookieAuthenticationDefaults.CookiePrefix}AntiForgery.{ComputeCookieName(applicationId)}";
            }
            else
            {
                options.Cookie.Name = $"{tenantId}{options.Cookie.Name}";
            }
        }
        private static string ComputeCookieName(string applicationId)
        {
            using (var sha256 = CreateSHA256())
            {
                var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(applicationId));
                var subHash = hash.Take(8).ToArray();
                return WebEncoders.Base64UrlEncode(subHash);
            }
        }
        private static SHA256 CreateSHA256()
        {
            try
            {
                return SHA256.Create();
            }
            // SHA256.Create is documented to throw this exception on FIPS compliant machines.
            // See: https://msdn.microsoft.com/en-us/library/z08hz7ad%28v=vs.110%29.aspx?f=255&MSPPError=-2147217396
            catch (System.Reflection.TargetInvocationException)
            {
                // Fallback to a FIPS compliant SHA256 algorithm.
                return new SHA256CryptoServiceProvider();
            }
        }
    }
}
