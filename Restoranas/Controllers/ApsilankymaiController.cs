using Microsoft.AspNetCore.Mvc;
using Restoranas.Models;
using System.Diagnostics;

namespace Restoranas.Controllers
{
    public class ApsilankymaiController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public ApsilankymaiController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Aktualiausi()
        {
            return View();
        }

        public IActionResult Meniu()
        {
            return View();
        }
        public IActionResult Praeje()
        {
            return View();
        }

    }
}