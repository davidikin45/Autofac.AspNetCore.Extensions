using Microsoft.AspNetCore.Mvc.Razor;
using System.Collections.Generic;

namespace Autofac.AspNetCore.Extensions.Mvc
{
    public class TenantViewLocationExpander : IViewLocationExpander
    {
        private const string ValueKey = "tenantId";

        public TenantViewLocationExpander()
        {
        }

        public void PopulateValues(ViewLocationExpanderContext context)
        {
            context.Values[ValueKey] = context.ActionContext.HttpContext.GetTenantId();
        }

        //The view locations passed to ExpandViewLocations are:
        // /Views/{1}/{0}.cshtml
        // /Shared/{0}.cshtml
        // /Pages/{0}.cshtml
        //Where {0} is the view and {1} the controller name.
        public virtual IEnumerable<string> ExpandViewLocations(ViewLocationExpanderContext context, IEnumerable<string> viewLocations)
        {
            foreach (var location in viewLocations)
            {
                if (!string.IsNullOrEmpty(context.Values[ValueKey]))
                {
                    yield return location.Replace("{0}", context.Values[ValueKey] + "/{0}");
                }

                yield return location;
            }
        }
    }
}
