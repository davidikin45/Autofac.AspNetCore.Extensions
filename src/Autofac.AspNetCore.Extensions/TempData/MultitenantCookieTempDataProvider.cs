using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Mvc.ViewFeatures.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;

namespace Autofac.AspNetCore.Extensions.TempData
{
    public class MultitenantCookieTempDataProvider : ITempDataProvider
    {
        private readonly IDataProtectionProvider _dataProtectionProvider;
        private readonly ILoggerFactory _loggerFactory;

        private readonly TempDataSerializer _tempDataSerializer;

        public MultitenantCookieTempDataProvider(
          IDataProtectionProvider dataProtectionProvider,
          ILoggerFactory loggerFactory

          ,TempDataSerializer tempDataSerializer

            )
        {
            _dataProtectionProvider = dataProtectionProvider;
            _loggerFactory = loggerFactory;

            _tempDataSerializer = tempDataSerializer;
        }

        private ITempDataProvider GetProvider(HttpContext httpContext)
        {
            if (httpContext.Items.ContainsKey("_multitenantCookieTempDataProvider"))
                return httpContext.Items["_multitenantCookieTempDataProvider"] as ITempDataProvider;

            var options = httpContext.RequestServices.GetRequiredService<IOptions<CookieTempDataProviderOptions>>();

            var cookieTempDataProvider = new CookieTempDataProvider(_dataProtectionProvider, _loggerFactory, options, _tempDataSerializer);

            //.NET Core 2.2
            //var cookieTempDataProvider = new CookieTempDataProvider(_dataProtectionProvider, _loggerFactory, options);

            httpContext.Items["_multitenantCookieTempDataProvider"] = cookieTempDataProvider;

            return cookieTempDataProvider;
        }

        public IDictionary<string, object> LoadTempData(HttpContext context)
        {
            var tempData = GetProvider(context).LoadTempData(context);
            if (tempData.ContainsKey("tenantId") && tempData["tenantId"] as string != context.GetTenantId())
                tempData = new Dictionary<string, Object>();

            tempData.Remove("tenantId");

            return tempData;
        }

        public void SaveTempData(HttpContext context, IDictionary<string, object> values)
        {
            values["tenantId"] = context.GetTenantId();
            GetProvider(context).SaveTempData(context, values);
        }
    }
}
