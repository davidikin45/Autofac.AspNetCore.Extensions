using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Autofac.AspNetCore.Extensions.Middleware
{
    public class MultitenantStaticFilesRewriteMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IHostingEnvironment _environment;
        private readonly ILogger _logger;

        public MultitenantStaticFilesRewriteMiddleware(
            RequestDelegate next,
            ILoggerFactory loggerFactory,
            IHostingEnvironment environment
            )
        {
            _next = next;
            _logger = loggerFactory.CreateLogger<MultitenantStaticFilesRewriteMiddleware>();
            _environment = environment;
        }

        public async Task Invoke(HttpContext context)
        {
            var tenantId = context.GetTenantId();

            var options = context.RequestServices.GetRequiredService<IOptions<MultitenantStaticFilesRewriteOptions>>().Value;

            bool pathChanged = false;
            var originalPath = context.Request.Path;

            if (originalPath.Value.StartsWith($"/{options.MapFrom}"))
            {
                var tenantFolder = options.TenantFolderResolver(tenantId);

                var newPath = new PathString(ReplaceFirstOccurrence(originalPath.Value, $"/{options.MapFrom}", $"/{options.MapTo}/{tenantFolder}/").Replace($"///", "/").Replace($"//", "/"));

                if (newPath.Value != originalPath && (options.ServeUnknownFileTypes || Path.GetExtension(newPath.Value) != string.Empty))
                {

                    var filePath = newPath.Value;
                    if (filePath.StartsWith("/", StringComparison.Ordinal))
                    {
                        filePath = filePath.Substring(1);
                    }
                    var path = Path.Combine(_environment.WebRootPath, filePath.Replace('/', '\\'));
                    if (originalPath.Value.Contains($"/{options.MapTo}") || File.Exists(path))
                    {
                        pathChanged = true;
                        context.Request.Path = newPath;

                        _logger.LogDebug($"Tenant specific static file requested, path rewritten from {originalPath} to {newPath} ");
                    }
                }
            }

            try
            {
                await _next(context);
            }
            finally
            {
                if(pathChanged)
                { 
                    //replace the original url after the remaing middleware has finished processing
                    context.Request.Path = originalPath;
                }
            }          
        }

        private static string ReplaceFirstOccurrence(string source, string find, string replace)
        {
            int index = source.IndexOf(find, StringComparison.InvariantCultureIgnoreCase);
            if (index > -1)
            {
                string result = source.Remove(index, find.Length).Insert(index, replace);
                return result;
            }
            else
            {
                return source;
            }
        }

    }

    public class MultitenantStaticFilesRewriteOptions
    {
        public bool ServeUnknownFileTypes { get; set; } = false;
        public string MapFrom { get; set; } = "";
        public string MapTo { get; set; } = "tenants";

        public Func<string, string> TenantFolderResolver { get; set; } = (tenantId) => tenantId;
    }
}
