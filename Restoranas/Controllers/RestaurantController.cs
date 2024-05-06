using Microsoft.AspNetCore.Mvc;

namespace Restoranas.Controllers
{
    public class RestaurantController : Controller
    {
        public IActionResult OpenRestaurantInfo()
        {
            return View("~/Views/Manager/RestaurantInfoPage.cshtml");
        }
    }
}
