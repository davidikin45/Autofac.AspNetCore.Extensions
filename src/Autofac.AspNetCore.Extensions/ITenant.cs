using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Text;

namespace Autofac.AspNetCore.Extensions
{
    public interface ITenant
    {
        string Id { get; }


        IConfiguration Configuration {get;}
    }
}
