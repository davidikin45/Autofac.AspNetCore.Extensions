# Autofac ASP.NET Core Extensions
[![nuget](https://img.shields.io/nuget/v/Autofac.AspNetCore.Extensions.svg)](https://www.nuget.org/packages/Autofac.AspNetCore.Extensions/) ![Downloads](https://img.shields.io/nuget/dt/Autofac.AspNetCore.Extensions.svg "Downloads")

## Installation

### NuGet
```
PM> Install-Package Autofac.AspNetCore.Extensions
```

### .Net CLI
```
> dotnet add package Autofac.AspNetCore.Extensions
```

## Single Tenant Usage ASP.NET Core 2.2
```
public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
WebHost.CreateDefaultBuilder(args)
.UseAutofac()
.UseStartup<Startup>();
```

## Single Tenant Usage ASP.NET Core 3.0
```
public static IHostBuilder CreateHostBuilder(string[] args) =>
Host.CreateDefaultBuilder(args)
.ConfigureWebHostDefaults(webBuilder =>
{
	webBuilder
	.UseAutofac()
	.UseStartup<Startup>();
});
```

## Multitenant Usage ASP.NET Core 2.2
```
public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
WebHost.CreateDefaultBuilder(args)
.ConfigureServices(services =>
{
	services.AddHttpContextAccessor();
	services.AddScoped<ITenantIdentificationStrategy, SubdomainIdentificationStrategy>();
})
.UseAutofacMultitenant()
.UseStartup<Startup>();

public class SubdomainIdentificationStrategy : ITenantIdentificationStrategy
{
	private readonly IHttpContextAccessor _httpContextAccessor;
	public SubdomainIdentificationStrategy(IHttpContextAccessor httpContextAccessor)
	{
		_httpContextAccessor = httpContextAccessor;
	}

	public bool TryIdentifyTenant(out object tenantId)
	{
		var context = _httpContextAccessor.HttpContext;
		if(context == null)
		{
			tenantId = null;
			return false;
		}

		tenantId = GetSubDomain(context);
		return tenantId != null;
	}

	private string GetSubDomain(HttpContext httpContext)
	{
		var subDomain = string.Empty;

		var host = httpContext.Request.Host.Host;

		if (!string.IsNullOrWhiteSpace(host))
		{
			subDomain = host.Split('.')[0];
		}

		return subDomain.Trim().ToLower();
	}
}
```

## Multitenant Usage ASP.NET Core 3.0
```
public static IHostBuilder CreateHostBuilder(string[] args) =>
Host.CreateDefaultBuilder(args)
.ConfigureWebHostDefaults(webBuilder =>
{
	webBuilder
	.ConfigureServices(services =>
	{
		services.AddHttpContextAccessor();
		services.AddScoped<ITenantIdentificationStrategy, SubdomainIdentificationStrategy>();
	})
	.UseAutofacMultitenant()
	.UseStartup<Startup>();
});

public class SubdomainIdentificationStrategy : ITenantIdentificationStrategy
{
	private readonly IHttpContextAccessor _httpContextAccessor;
	public SubdomainIdentificationStrategy(IHttpContextAccessor httpContextAccessor)
	{
		_httpContextAccessor = httpContextAccessor;
	}

	public bool TryIdentifyTenant(out object tenantId)
	{
		var context = _httpContextAccessor.HttpContext;
		if(context == null)
		{
			tenantId = null;
			return false;
		}

		tenantId = GetSubDomain(context);
		return tenantId != null;
	}

	private string GetSubDomain(HttpContext httpContext)
	{
		var subDomain = string.Empty;

		var host = httpContext.Request.Host.Host;

		if (!string.IsNullOrWhiteSpace(host))
		{
			subDomain = host.Split('.')[0];
		}

		return subDomain.Trim().ToLower();
	}
}
```

## Authors

* **Dave Ikin** - [davidikin45](https://github.com/davidikin45)


## License

This project is licensed under the MIT License