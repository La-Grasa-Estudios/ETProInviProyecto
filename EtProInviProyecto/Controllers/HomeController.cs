using ETPro.Data;
using EtProInviProyecto.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EtPro.Controllers
{
    public class HomeController : Controller
    {
        private readonly AppDbContext _context;
        public HomeController(AppDbContext context)
        {
            _context = context;
        }
        public IActionResult Index()
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Dashboard", "Home");
            }

            return View();
        }

        public async Task<IActionResult> Dashboard()
        {
            var bienesTotales = _context.Bienes.Count();
            int bienesActivos = await _context.Bienes.CountAsync(b => b.Activo == true);
            int bienesEnTraspaso = await _context.Movements.CountAsync(b => b.Type == Models.MovementType.Traspaso);
            BienesRegistradosViewModel bienesRegistradosViewModel = new BienesRegistradosViewModel();
            bienesRegistradosViewModel.TotalBienes = bienesTotales;
            bienesRegistradosViewModel.TotalActivos = bienesActivos;
            bienesRegistradosViewModel.TotalMantenimiento = bienesEnTraspaso;
            return View(bienesRegistradosViewModel);
        }

        public async Task<IActionResult> Bienes()
        {
            var bienesTotales = _context.Bienes.Count();
            int bienesActivos = await _context.Bienes.CountAsync(b => b.Activo == true);
            BienesRegistradosViewModel bienesRegistradosViewModel = new BienesRegistradosViewModel();
            bienesRegistradosViewModel.TotalBienes = bienesTotales;
            bienesRegistradosViewModel.TotalActivos = bienesActivos;
            return View(bienesRegistradosViewModel);
        }
    }
}