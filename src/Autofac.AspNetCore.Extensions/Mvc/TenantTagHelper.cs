using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using System.Threading.Tasks;

namespace Autofac.AspNetCore.Extensions.Mvc
{
    [HtmlTargetElement("tenant")]
    public sealed class TenantTagHelper : TagHelper
    {
        [HtmlAttributeNotBound, ViewContext]
        public ViewContext ViewContext { get; set; }

        [HtmlAttributeName("tenant-id")]
        public string TenantId { get; set; }

        public override Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
        {
            if (TenantId != ViewContext.HttpContext.GetTenantId())
            {
                output.SuppressOutput();
            }

            return base.ProcessAsync(context, output);
        }
    }
}
