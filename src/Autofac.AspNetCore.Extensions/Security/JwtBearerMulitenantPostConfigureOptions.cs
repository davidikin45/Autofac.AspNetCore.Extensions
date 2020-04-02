using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;

namespace Autofac.AspNetCore.Extensions.Security
{
    public class JwtBearerMultitenantPostConfigureOptions : IPostConfigureOptions<JwtBearerOptions>
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public JwtBearerMultitenantPostConfigureOptions(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public void PostConfigure(string name, JwtBearerOptions options)
        {
            options.TokenValidationParameters.IssuerSigningKeyResolver = (string token, SecurityToken securityToken, string kid, TokenValidationParameters validationParameters) =>
            {
                List<SecurityKey> keys = null;

                //kid must start with tenantId!
                //No two keys should have same kid

                var tenantId = _httpContextAccessor.HttpContext.GetTenantId() ?? string.Empty;

                keys = new List<SecurityKey>();

                var kidParts = kid.Split('.');

                if (!string.IsNullOrEmpty(kid) && ((kidParts.Length > 1 && kidParts[0] == tenantId) || kidParts[0].StartsWith(tenantId)))
                {
                    var key = new JwtSecurityTokenHandlerInner().ResolveIssuerSigningKey(token, securityToken as JwtSecurityToken, validationParameters);
                    if (key != null)
                    {
                        keys.Add(key);
                    }
                }

                //if (!string.IsNullOrEmpty(kid))
                //{
                //    if (!string.IsNullOrEmpty(kid) && kid.Split('.')[0] == tenantId)
                //    {
                //        var key = new JwtSecurityTokenHandlerInner().ResolveIssuerSigningKey(token, securityToken as JwtSecurityToken, validationParameters);
                //        if (key != null)
                //        {
                //            keys.Add(key);
                //        }
                //    }
                //}
                //else
                //{
                //    var key = new JwtSecurityTokenHandlerInner().ResolveIssuerSigningKey(token, securityToken as JwtSecurityToken, validationParameters);
                //    if (key != null)
                //    {
                //        keys = new List<SecurityKey> { key };
                //    }
                //}

                return keys;
            };
        }

        private class JwtSecurityTokenHandlerInner : JwtSecurityTokenHandler
        {
            public new SecurityKey ResolveIssuerSigningKey(string token, JwtSecurityToken jwtToken, TokenValidationParameters validationParameters)
            {
                return base.ResolveIssuerSigningKey(token, jwtToken, validationParameters);
            }
        }
    }
}
