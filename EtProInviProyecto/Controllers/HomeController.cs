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
                {
                    var deptosVisibles = await _context.Departments
                        .Where(d => d.ID == deptId || d.ParentDepartmentID == deptId)
                        .Select(d => d.ID)
                        .ToListAsync();

                    bienesVisibles = bienesVisibles.Where(b => deptosVisibles.Contains(b.DependenciaID));
                }
                else
                {
                    bienesVisibles = bienesVisibles.Where(b => false);
                }
            }

            int totalBienes = await bienesVisibles.CountAsync();
            int totalActivos = totalBienes;
            int totalDesincorporados = verTodos
                ? await _context.Bienes.CountAsync(b => !b.Activo)
                : (int.TryParse(deptClaim, out int dId)
                    ? await _context.Bienes.CountAsync(b => !b.Activo && b.DependenciaID == dId)
                    : 0);

            var idsVisibles = await bienesVisibles.Select(b => b.ID).ToListAsync();
            int totalMantenimiento = await _context.Movements
                .CountAsync(m => m.Type == MovementType.Traspaso && idsVisibles.Contains(m.BienId));

            var movimientosRecientes = await _context.Movements
                .Include(m => m.Bien)
                .Include(m => m.OriginDepartment)
                .Include(m => m.DestinationDepartment)
                .Include(m => m.UsuarioSolicitante)
                .Include(m => m.UsuarioAprobador)
                .Where(m => m.Estado == "Aprobado" && idsVisibles.Contains(m.BienId))
                .OrderByDescending(m => m.FechaAprobacion)
                .Take(5)
                .ToListAsync();

            int solicitudesPendientes = 0;
            if (User.HasClaim("Permiso", "Movimientos.AprobarTraspaso")
                || User.HasClaim("Permiso", "Movimientos.AprobarDesincorporacion")
                || User.HasClaim("Permiso", "Movimientos.AprobarIncorporacion"))
            {
                solicitudesPendientes = await _context.Movements
                    .CountAsync(m => m.Estado == "Pendiente");
            }

            int custodiosACargo = 0;
            string gerenteEncargado = null;

            var deptosComoGerente = await _context.Departments
                .Where(d => d.ManagerID == userId)
                .ToListAsync();
            if (deptosComoGerente.Any())
            {
                custodiosACargo = await _context.Departments
                    .CountAsync(d => deptosComoGerente.Select(x => x.ID).Contains(d.ID) && d.CustodianID != null);
            }

            var deptoComoCustodio = await _context.Departments
                .FirstOrDefaultAsync(d => d.CustodianID == userId);
            if (deptoComoCustodio != null && deptoComoCustodio.ManagerID != null)
            {
                var manager = await _context.Users.FindAsync(deptoComoCustodio.ManagerID);
                gerenteEncargado = manager?.FullName ?? manager?.UserName;
            }

            var model = new BienesRegistradosViewModel
            {
                TotalBienes = totalBienes,
                TotalActivos = totalActivos,
                TotalMantenimiento = totalMantenimiento,
                TotalDesincorporados = totalDesincorporados,
                MovimientosRecientes = movimientosRecientes,
                SolicitudesPendientes = solicitudesPendientes,
                CustodiosACargo = custodiosACargo,
                GerenteEncargado = gerenteEncargado
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