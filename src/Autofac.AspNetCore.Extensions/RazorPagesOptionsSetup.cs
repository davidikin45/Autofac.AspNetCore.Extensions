using Autofac.Extensions.DependencyInjection;
using Autofac.Multitenant;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using System.Reflection;

namespace Autofac.AspNetCore.Extensions
{
    public class RazorPagesOptionsSetup : IConfigureOptions<RazorPagesOptions>
    {
        private readonly IServiceProvider _serviceProvider;

        public RazorPagesOptionsSetup(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        private static FieldInfo MvcOptions = typeof(PageConventionCollection).GetField("_mvcOptions", BindingFlags.Instance | BindingFlags.NonPublic);

        public void Configure(RazorPagesOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            var mvcOptions = _serviceProvider.GetRequiredService<IOptions<MvcOptions>>().Value;
            MvcOptions.SetValue(options.Conventions, mvcOptions);
        }
    }
}
