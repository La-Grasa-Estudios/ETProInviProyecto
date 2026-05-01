using Microsoft.AspNetCore.Mvc;

namespace EtPro.Controllers
{
    [PermissionAuthorize("Bienes.VerTodos")]
    public class AdminController : Controller
    {
        public IActionResult Index()
        {
            return Content("Tienes permiso para ver todos los bienes");
        }
    }
}
