using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;

namespace Autofac.AspNetCore.Extensions.Security
{
    public class AntiForgeryMultitenantAdditionalData : IAntiforgeryAdditionalDataProvider
    {
        public string GetAdditionalData(HttpContext context)
        {
            return context.GetTenantId();
        }

        public bool ValidateAdditionalData(HttpContext context, string additionalData)
        {
            return (context.GetTenantId() ?? string.Empty) == additionalData;
        }
    }
}
