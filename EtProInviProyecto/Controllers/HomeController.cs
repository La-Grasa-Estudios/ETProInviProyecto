using EtPro.Data;
using EtPro.Models;
using EtProInviProyecto.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

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

        [PermissionAuthorize("Bienes.VerPropios")]
        public async Task<IActionResult> Dashboard()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var deptClaim = User.FindFirst("DepartmentId")?.Value;

            IQueryable<BienMueble> bienesVisibles = _context.Bienes.Where(b => b.Activo && b.Aprobado);

            bool verTodos = User.HasClaim("Permiso", "Bienes.VerTodos");

            if (!verTodos)
            {
                if (int.TryParse(deptClaim, out int deptId))
                    bienesVisibles = bienesVisibles.Where(b => b.DependenciaID == deptId);
                else
                    bienesVisibles = bienesVisibles.Where(b => false); 
            }

            int totalBienes = await bienesVisibles.CountAsync();
            int totalActivos = await bienesVisibles.CountAsync(b => b.Activo); 
            int totalDesincorporados = 0; 
            if (verTodos)
            {
                totalDesincorporados = await _context.Bienes.CountAsync(b => !b.Activo);
            }
            else if (int.TryParse(deptClaim, out int dId))
            {
                totalDesincorporados = await _context.Bienes.CountAsync(b => !b.Activo && b.DependenciaID == dId);
            }

            var idsVisibles = await bienesVisibles.Select(b => b.ID).ToListAsync();
            int totalMantenimiento = await _context.Movements
                .CountAsync(m => m.Type == MovementType.Traspaso && idsVisibles.Contains(m.BienId));

            var model = new BienesRegistradosViewModel
            {
                TotalBienes = totalBienes,
                TotalActivos = totalActivos,
                TotalMantenimiento = totalMantenimiento,
                TotalDesincorporados = totalDesincorporados
            };

            return View(model);
        }

        [PermissionAuthorize("Bienes.VerPropios")]
        public async Task<IActionResult> Bienes()
        {

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var deptClaim = User.FindFirst("DepartmentId")?.Value;

            IQueryable<BienMueble> query = _context.Bienes.Where(b => b.Activo && b.Aprobado);

            if (!User.HasClaim("Permiso", "Bienes.VerTodos"))
            {
                if (int.TryParse(deptClaim, out int deptId))
                    query = query.Where(b => b.DependenciaID == deptId);
                else
                    query = query.Where(b => false);
            }

            var model = new BienesRegistradosViewModel
            {
                TotalBienes = await query.CountAsync(),
                TotalActivos = await query.CountAsync(b => b.Activo)
            };
            return View(model);
        }
    }
}