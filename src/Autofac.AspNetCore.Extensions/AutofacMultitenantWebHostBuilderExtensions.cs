using Microsoft.AspNetCore.Hosting;
using System;

namespace Autofac.AspNetCore.Extensions
{
    public static class AutofacMultitenantWebHostBuilderExtensions
    {
        internal static IWebHostBuilder UseAutofacMultitenantRequestServices(this IWebHostBuilder builder)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));

            return builder.ConfigureServices(services =>
            {
                services.AddAutofacMultitenantRequestServices();
            });
        }
    }
}
