using System;
using System.Collections.Generic;
using System.Text;

namespace Autofac.AspNetCore.Extensions.Data
{
    public interface IEntityTenantSchema
    {

    }

    public interface IEntityTenantFilter
    {
        string TenantId { get; set; }
    }

    public interface IEntityTenantFilterShadowProperty
    {

    }

    public interface IEntityTenant
    {

    }
}
