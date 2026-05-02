using EtPro.Models;
using ETPro.Data;
using ETPro.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace EtPro.Controllers
{
    [Authorize]
    public class BienesController : Controller
    {
        private readonly AppDbContext _context;

        public BienesController(AppDbContext context)
        {
            _context = context;
        }

        [PermissionAuthorize("Bienes.VerPropios")]
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var deptIdClaim = User.FindFirst("DepartmentId")?.Value;

            var query = _context.Bienes.Where(b => b.Activo && b.Aprobado);

            if (!User.HasClaim("Permiso", "Bienes.VerTodos"))
            {
                if (int.TryParse(deptIdClaim, out int deptId))
                    query = query.Where(b => b.DependenciaID == deptId);
                else
                    return Forbid();
            }

            var bienes = await query.ToListAsync();
            return View(bienes);
        }

        [HttpGet]
        [PermissionAuthorize("Movimientos.AprobarIncorporacion")]
        public async Task<IActionResult> PendientesIncorporacion()
        {
            var movimientos = await _context.Movements
                .Include(m => m.Bien)
                .Include(m => m.OriginDepartment)
                .Include(m => m.UsuarioSolicitante)
                .Where(m => m.Type == MovementType.Incorporacion && m.Estado == "Pendiente")
                .OrderByDescending(m => m.FechaSolicitud)
                .ToListAsync();

            return View(movimientos);
        }

        [HttpPost]
        [PermissionAuthorize("Movimientos.AprobarIncorporacion")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AprobarIncorporacion(int id)
        {
            var movimiento = await _context.Movements
                .Include(m => m.Bien)
                .FirstOrDefaultAsync(m => m.Id == id && m.Type == MovementType.Incorporacion && m.Estado == "Pendiente");

            if (movimiento == null) return NotFound();

            if (movimiento.Bien != null)
            {
                movimiento.Bien.Aprobado = true;
                _context.Bienes.Update(movimiento.Bien);
            }

            movimiento.Estado = "Aprobado";
            movimiento.UsuarioAprobadorId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            movimiento.FechaAprobacion = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            TempData["Success"] = "Incorporación aprobada correctamente.";
            return RedirectToAction(nameof(PendientesIncorporacion));
        }

        [HttpPost]
        [PermissionAuthorize("Movimientos.AprobarIncorporacion")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RechazarIncorporacion(int id)
        {
            var movimiento = await _context.Movements
                .Include(m => m.Bien)
                .FirstOrDefaultAsync(m => m.Id == id && m.Type == MovementType.Incorporacion && m.Estado == "Pendiente");

            if (movimiento == null) return NotFound();

            if (movimiento.Bien != null)
            {
                _context.Bienes.Remove(movimiento.Bien);
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = "Incorporación rechazada.";
            return RedirectToAction(nameof(PendientesIncorporacion));
        }

        [HttpGet]
        [PermissionAuthorize("Bienes.Crear")]
        public IActionResult SolicitarIncorporacion()
        {
            ViewBag.Departments = new SelectList(_context.Departments, "ID", "Name");
            return View(new SolicitarIncorporacionViewModel());
        }

        [HttpPost]
        [PermissionAuthorize("Bienes.Crear")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SolicitarIncorporacion(SolicitarIncorporacionViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Departments = new SelectList(_context.Departments, "ID", "Name");
                return View(model);
            }

            var nuevoBien = new BienMueble
            {
                NumeroIdentificacion = model.NumeroIdentificacion,
                Nombre = model.Nombre,
                Marca = model.Marca,
                Modelo = model.Modelo,
                Serial = model.Serial,
                Color = model.Color,
                Material = model.Material,
                ObservacionesAdicionales = model.ObservacionesAdicionales,
                Grupo = model.Grupo,
                DependenciaID = model.DependenciaID,
                ValorUnitario = model.ValorUnitario,
                Activo = true,       
                Aprobado = false     
            };

            _context.Bienes.Add(nuevoBien);
            await _context.SaveChangesAsync();

            var movimiento = new Movement
            {
                BienId = nuevoBien.ID,
                Type = MovementType.Incorporacion,
                OriginDepartmentId = nuevoBien.DependenciaID,
                FechaSolicitud = DateTime.UtcNow,
                Estado = "Pendiente",
                UsuarioSolicitanteId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                Motivo = "Incorporación de nuevo bien"  
            };

            _context.Movements.Add(movimiento);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Solicitud de incorporación registrada. El bien está pendiente de aprobación.";
            return RedirectToAction(nameof(SolicitarIncorporacion));
        }

        [PermissionAuthorize("Bienes.VerPropios")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var bien = await _context.Bienes.FindAsync(id);
            if (bien == null) return NotFound();

            var userID = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var deptIDClaim = User.FindFirst("DepartamentoId")?.Value;

            bool verTodos = await _context.UserPermission
                .AnyAsync(up => up.UserID == userID && up.Permission.Name == "Bienes.VerTodos");

            if (!verTodos)
            {
                if (int.TryParse(deptIDClaim, out int deptID))
                {
                    if (bien.DependenciaID != deptID)
                        return Forbid();   
                }
                else
                {
                    return Forbid();      
                }
            }

            return View(bien);
        }

        [PermissionAuthorize("Bienes.Crear")]
        public IActionResult Create()
        {
            ViewBag.Departments = new SelectList(_context.Departments, "ID", "Name");
            return View();
        }

        [HttpPost]
        [PermissionAuthorize("Bienes.Crear")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(BienMueble bien)
        {
            if (ModelState.IsValid)
            {
                _context.Bienes.Add(bien);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewBag.Departments = new SelectList(_context.Departments, "ID", "Name", bien.DependenciaID);
            return View(bien);
        }

        [PermissionAuthorize("Bienes.Editar")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var bien = await _context.Bienes.FindAsync(id);
            if (bien == null || !bien.Activo) return NotFound();

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var deptIdClaim = User.FindFirst("DepartmentId")?.Value;

            bool editarTodos = await _context.UserPermission
                .AnyAsync(up => up.UserID == userId && up.Permission.Name == "Bienes.Editar");

            if (!editarTodos)
            {
                if (int.TryParse(deptIdClaim, out int deptId))
                {
                    if (bien.DependenciaID != deptId)
                        return Forbid();
                }
                else
                {
                    return Forbid(); 
                }
            }

            ViewBag.Departments = new SelectList(_context.Departments, "ID", "Name", bien.DependenciaID);
            return View(bien);
        }

        [HttpPost]
        [PermissionAuthorize("Bienes.Editar")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int ID)
        {
            if (ID == null) return NotFound();

            var bien = await _context.Bienes.FindAsync(ID);
            if (bien == null || !bien.Activo) return NotFound();

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var deptIdClaim = User.FindFirst("DepartmentId")?.Value;

            bool editarTodos = await _context.UserPermission
                .AnyAsync(up => up.UserID == userId && up.Permission.Name == "Bienes.Editar");

            if (!editarTodos)
            {
                if (int.TryParse(deptIdClaim, out int deptId))
                {
                    if (bien.DependenciaID != deptId)
                        return Forbid();
                }
                else
                {
                    return Forbid(); 
                }
            }

            ViewBag.Departments = new SelectList(_context.Departments, "ID", "Name", bien.DependenciaID);
            return View(bien);
        }

        //[PermissionAuthorize("Bienes.Desincorporar")]
        //public async Task<IActionResult> Delete(int? ID)
        //{
        //    if (ID == null) return NotFound();
        //
        //    var bien = await _context.Bienes.FindAsync(ID);
        //    if (bien == null) return NotFound();
        //
        //    var userID = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        //    var deptIDClaim = User.FindFirst("DepartamentoId")?.Value;
        //
        //    bool desincorporarTodos = await _context.UserPermission
        //        .AnyAsync(up => up.UserID == userID && up.Permission.Name == "Bienes.Desincorporar");
        //
        //    if (!desincorporarTodos)
        //    {
        //        if (int.TryParse(deptIDClaim, out int deptID))
        //        {
        //            if (bien.DependenciaID != deptID)
        //                return Forbid();
        //        }
        //        else
        //        {
        //            return Forbid();
        //        }
        //    }
        //
        //    return View(bien);
        //}
        //
        //[HttpPost, ActionName("Delete")]
        //[PermissionAuthorize("Bienes.Desincorporar")]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> DeleteConfirmed(int ID)
        //{
        //    var bien = await _context.Bienes.FindAsync(ID);
        //    if (bien != null)
        //    {
        //        _context.Bienes.Remove(bien);
        //        await _context.SaveChangesAsync();
        //    }
        //    return RedirectToAction(nameof(Index));
        //}
    }
}