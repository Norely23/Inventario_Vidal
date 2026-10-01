using Microsoft.AspNetCore.Mvc;

namespace Inventario_Vidal.Controllers
{
    public class HomeController1 : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
