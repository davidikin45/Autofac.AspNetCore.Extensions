using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Autofac.AspNetCore.Extensions;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WebApplication35.Models;
using WebApplication35.Settings;

namespace WebApplication35.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private static IOptionsFactory<CookieAuthenticationOptions> _fact;
        public HomeController(ILogger<HomeController> logger, IOptionsFactory<CookieAuthenticationOptions> fact, IOptionsMonitor<AppSettings> appSettings, ITenant tenant)
        {
            var options = appSettings.CurrentValue;
            
          
            
            _logger = logger;
            _fact = fact;
        }

        public IActionResult Index()
        {
            TempData["abc"] = HttpContext.GetTenantId();
            return View();
        }

        public IActionResult Privacy()
        {
            var a = TempData["abc"];
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
