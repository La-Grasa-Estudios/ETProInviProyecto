using Microsoft.AspNetCore.Mvc;

namespace EtPro.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}