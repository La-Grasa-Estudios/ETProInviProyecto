using EtPro.Models;

using EtPro.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace EtPro.Controllers
{
    [Authorize]
    public class MovementsController : Controller
    {
        private readonly AppDbContext _context;

        public MovementsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        [PermissionAuthorize("Movimientos.SolicitarTraspaso")]

        private async Task CargarDropdownsTraspaso(int? bienIdSeleccionado = null, int? destinoSeleccionado = null)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var deptIdClaim = User.FindFirst("DepartmentId")?.Value;

            var query = _context.Bienes.Where(b => b.Activo);
            bool verTodos = await _context.UserPermission
                .AnyAsync(up => up.UserID == userId && up.Permission.Name == "Bienes.VerTodos");

            if (!verTodos)
            {
                if (int.TryParse(deptIdClaim, out int deptId))
                    query = query.Where(b => b.DependenciaID == deptId);
            }
            var bienes = await query.ToListAsync();

            int? currentDept = null;
            if (int.TryParse(deptIdClaim, out int dId))
                currentDept = dId;

            var departamentosDestino = await _context.Departments
                .Where(d => d.ID != currentDept)
                .ToListAsync();

            ViewBag.Bienes = new SelectList(bienes, "ID", "NumeroIdentificacion", bienIdSeleccionado);
            ViewBag.Departments = new SelectList(departamentosDestino, "ID", "Name", destinoSeleccionado);
        }

        private async Task CargarDropdownsDesincorporacion(int? bienIdSeleccionado = null)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var deptIdClaim = User.FindFirst("DepartmentId")?.Value;

            var query = _context.Bienes.Where(b => b.Activo);
            bool verTodos = await _context.UserPermission
                .AnyAsync(up => up.UserID == userId && up.Permission.Name == "Bienes.VerTodos");

            if (!verTodos)
            {
                if (int.TryParse(deptIdClaim, out int deptId))
                    query = query.Where(b => b.DependenciaID == deptId);
            }
            var bienes = await query.ToListAsync();

            ViewBag.Bienes = new SelectList(bienes, "ID", "NumeroIdentificacion", bienIdSeleccionado);
        }



        [HttpGet]
        [PermissionAuthorize("Bienes.Crear")]
        public IActionResult SolicitarIncorporacion()
        {
            ViewBag.Departments = new SelectList(_context.Departments, "ID", "Name");

            string ultimoCodigo = _context.Bienes
                .Where(b => b.NumeroIdentificacion.StartsWith("ET-"))
                .OrderByDescending(b => b.NumeroIdentificacion)
                .Select(b => b.NumeroIdentificacion)
                .FirstOrDefault();

            string siguienteCodigo = "ET-00001";
            if (!string.IsNullOrEmpty(ultimoCodigo))
            {
                string parteNumerica = ultimoCodigo.Replace("ET-", "");
                if (int.TryParse(parteNumerica, out int numero))
                    siguienteCodigo = $"ET-{(numero + 1).ToString("D5")}";
            }

            var model = new SolicitarIncorporacionViewModel
            {
                NumeroIdentificacion = siguienteCodigo
            };

            return View(model);
        }

        [HttpGet]
        [PermissionAuthorize("Historial.VerPropios")]
        public async Task<IActionResult> HistorialTraspasos()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var deptClaim = User.FindFirst("DepartmentId")?.Value;

            var query = _context.Movements
                .Include(m => m.Bien)
                .Include(m => m.OriginDepartment)
                .Include(m => m.DestinationDepartment)
                .Include(m => m.UsuarioSolicitante)
                .Include(m => m.UsuarioAprobador)
                .Where(m => m.Type == MovementType.Traspaso && m.Estado == "Aprobado")
                .AsQueryable();

            if (!User.HasClaim("Permiso", "Historial.VerTodos"))
            {
                int? userDept = null;
                if (int.TryParse(deptClaim, out int d))
                    userDept = d;
                else
                    return Forbid();

                query = query.Where(m => m.Bien.DependenciaID == userDept);
            }

            var movimientos = await query
                .OrderByDescending(m => m.FechaAprobacion)
                .ToListAsync();

            return View(movimientos);
        }

        public async Task<IActionResult> SolicitarTraspaso()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var deptIdClaim = User.FindFirst("DepartmentId")?.Value;

            var query = _context.Bienes.Where(b => b.Activo);
            bool verTodos = await _context.UserPermission
                .AnyAsync(up => up.UserID == userId && up.Permission.Name == "Bienes.VerTodos");

            if (!verTodos)
            {
                if (int.TryParse(deptIdClaim, out int deptId))
                    query = query.Where(b => b.DependenciaID == deptId);
                else
                    return Forbid();  
            }

            var bienes = await query.ToListAsync();


            int? currentDept = null;
            if (int.TryParse(deptIdClaim, out int dId))
                currentDept = dId;

            var departamentosDestino = await _context.Departments
                .Where(d => d.ID != currentDept)
                .ToListAsync();

            ViewBag.Bienes = new SelectList(bienes, "ID", "NumeroIdentificacion");
            ViewBag.Departments = new SelectList(departamentosDestino, "ID", "Name");
            return View(new SolicitarTraspasoViewModel());
        }

        [HttpPost]
        [PermissionAuthorize("Movimientos.SolicitarTraspaso")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SolicitarTraspaso(SolicitarTraspasoViewModel model)
        {
            if (!ModelState.IsValid)
            {
                await CargarDropdownsTraspaso(model.BienId, model.DestinationDepartmentId);
                return View(model);
            }

            var movement = new Movement
            {
                BienId = model.BienId,
                DestinationDepartmentId = model.DestinationDepartmentId,
                Motivo = model.Motivo,
                Type = MovementType.Traspaso,
                FechaSolicitud = DateTime.UtcNow,
                Estado = "Pendiente",
                UsuarioSolicitanteId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            };

            var bien = await _context.Bienes.FindAsync(movement.BienId);
            if (bien != null)
                movement.OriginDepartmentId = bien.DependenciaID;

            _context.Movements.Add(movement);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Solicitud de traspaso registrada exitosamente.";
            return RedirectToAction(nameof(SolicitarTraspaso));
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

            bool codigoExiste = await _context.Bienes.AnyAsync(b => b.NumeroIdentificacion == model.NumeroIdentificacion);
            if (codigoExiste)
            {
                ModelState.AddModelError(nameof(model.NumeroIdentificacion), "Este número de identificación ya existe. Debe ser único.");
                ViewBag.Departments = new SelectList(_context.Departments, "ID", "Name");
                return View(model);
            }

            bool registroDirecto = User.HasClaim("Permiso", "Bienes.RegistroDirecto");

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
                Subgrupo = model.Subgrupo,
                Seccion = model.Seccion,
                FechaRegistro = DateTime.UtcNow,
                Aprobado = registroDirecto  
            };

            _context.Bienes.Add(nuevoBien);
            await _context.SaveChangesAsync();

            var movimiento = new Movement
            {
                BienId = nuevoBien.ID,
                Type = MovementType.Incorporacion,
                OriginDepartmentId = nuevoBien.DependenciaID,
                Motivo = registroDirecto ? "Registro directo" : "Solicitud de incorporación",
                FechaSolicitud = DateTime.UtcNow,
                Estado = registroDirecto ? "Aprobado" : "Pendiente",
                UsuarioSolicitanteId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                UsuarioAprobadorId = registroDirecto ? User.FindFirst(ClaimTypes.NameIdentifier)?.Value : null,
                FechaAprobacion = registroDirecto ? DateTime.UtcNow : null
            };

            _context.Movements.Add(movimiento);
            await _context.SaveChangesAsync();

            TempData["Success"] = registroDirecto
                ? "Bien incorporado exitosamente"
                : "Solicitud de incorporación registrada. El bien está pendiente de aprobación.";

            return RedirectToAction(nameof(SolicitarIncorporacion));
        }

        [HttpGet]
        [PermissionAuthorize("Bienes.Desincorporar")]
        public async Task<IActionResult> SolicitarDesincorporacion()
        {
            await CargarDropdownsDesincorporacion();
            return View(new SolicitarDesincorporacionViewModel());
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
                movimiento.Bien.Activo = false;
                _context.Bienes.Update(movimiento.Bien);
            }

            movimiento.Estado = "Rechazado";
            movimiento.UsuarioAprobadorId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            movimiento.FechaAprobacion = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            TempData["Success"] = "Incorporación rechazada.";
            return RedirectToAction(nameof(PendientesIncorporacion));
        }

        [HttpPost]
        [PermissionAuthorize("Bienes.Desincorporar")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SolicitarDesincorporacion(SolicitarDesincorporacionViewModel model)
        {
            if (!ModelState.IsValid)
            {
                await CargarDropdownsDesincorporacion(model.BienId);
                return View(model);
            }

            var movement = new Movement
            {
                BienId = model.BienId,
                Motivo = model.Motivo,
                Type = MovementType.Desincorporacion,
                FechaSolicitud = DateTime.UtcNow,
                Estado = "Pendiente",
                UsuarioSolicitanteId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            };

            var bien = await _context.Bienes.FindAsync(movement.BienId);
            if (bien != null)
                movement.OriginDepartmentId = bien.DependenciaID;

            _context.Movements.Add(movement);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Solicitud de desincorporación registrada exitosamente.";
            return RedirectToAction(nameof(SolicitarDesincorporacion));
        }

        [HttpGet]
        [PermissionAuthorize("Movimientos.AprobarTraspaso")]
        public async Task<IActionResult> PendientesTraspaso()
        {
            var movimientos = await _context.Movements
                .Include(m => m.Bien)
                .Include(m => m.OriginDepartment)
                .Include(m => m.DestinationDepartment)
                .Include(m => m.UsuarioSolicitante)
                .Where(m => m.Type == MovementType.Traspaso && m.Estado == "Pendiente")
                .OrderByDescending(m => m.FechaSolicitud)
                .ToListAsync();

            return View(movimientos);
        }

        [HttpPost]
        [PermissionAuthorize("Movimientos.AprobarTraspaso")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AprobarTraspaso(int id)
        {
            var movimiento = await _context.Movements
                .Include(m => m.Bien)
                .FirstOrDefaultAsync(m => m.Id == id && m.Type == MovementType.Traspaso && m.Estado == "Pendiente");

            if (movimiento == null) return NotFound();

            movimiento.Estado = "Aprobado";
            movimiento.UsuarioAprobadorId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            movimiento.FechaAprobacion = DateTime.UtcNow;

            if (movimiento.Bien != null && movimiento.DestinationDepartmentId.HasValue)
            {
                movimiento.Bien.DependenciaID = movimiento.DestinationDepartmentId.Value;
                _context.Bienes.Update(movimiento.Bien);
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = "Traspaso aprobado correctamente.";
            return RedirectToAction(nameof(PendientesTraspaso));
        }

        [HttpPost]
        [PermissionAuthorize("Movimientos.AprobarTraspaso")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RechazarTraspaso(int id)
        {
            var movimiento = await _context.Movements
                .FirstOrDefaultAsync(m => m.Id == id && m.Type == MovementType.Traspaso && m.Estado == "Pendiente");

            if (movimiento == null) return NotFound();

            movimiento.Estado = "Rechazado";
            movimiento.UsuarioAprobadorId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            movimiento.FechaAprobacion = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            TempData["Success"] = "Traspaso rechazado.";
            return RedirectToAction(nameof(PendientesTraspaso));
        }

        [HttpGet]
        [PermissionAuthorize("Movimientos.AprobarDesincorporacion")]
        public async Task<IActionResult> PendientesDesincorporacion()
        {
            var movimientos = await _context.Movements
                .Include(m => m.Bien)
                .Include(m => m.OriginDepartment)
                .Include(m => m.UsuarioSolicitante)
                .Where(m => m.Type == MovementType.Desincorporacion && m.Estado == "Pendiente")
                .OrderByDescending(m => m.FechaSolicitud)
                .ToListAsync();

            return View(movimientos);
        }

        [HttpPost]
        [PermissionAuthorize("Movimientos.AprobarDesincorporacion")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AprobarDesincorporacion(int id)
        {
            var movimiento = await _context.Movements
                .Include(m => m.Bien)
                .FirstOrDefaultAsync(m => m.Id == id && m.Type == MovementType.Desincorporacion && m.Estado == "Pendiente");

            if (movimiento == null) return NotFound();

            movimiento.Estado = "Aprobado";
            movimiento.UsuarioAprobadorId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            movimiento.FechaAprobacion = DateTime.UtcNow;

            if (movimiento.Bien != null)
            {
                movimiento.Bien.Activo = false;
                _context.Bienes.Update(movimiento.Bien);
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = "Desincorporación aprobada.";
            return RedirectToAction(nameof(PendientesDesincorporacion));
        }

        [HttpPost]
        [PermissionAuthorize("Movimientos.AprobarDesincorporacion")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RechazarDesincorporacion(int id)
        {
            var movimiento = await _context.Movements
                .FirstOrDefaultAsync(m => m.Id == id && m.Type == MovementType.Desincorporacion && m.Estado == "Pendiente");

            if (movimiento == null) return NotFound();

            movimiento.Estado = "Rechazado";
            movimiento.UsuarioAprobadorId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            movimiento.FechaAprobacion = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            TempData["Success"] = "Desincorporación rechazada.";
            return RedirectToAction(nameof(PendientesDesincorporacion));
        }

        [HttpGet]
        [PermissionAuthorize("Historial.VerPropios")]
        public async Task<IActionResult> Historial(int bienId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var deptClaim = User.FindFirst("DepartmentId")?.Value;

            var movimientos = await _context.Movements
                .Include(m => m.Bien)
                .Include(m => m.OriginDepartment)
                .Include(m => m.DestinationDepartment)
                .Include(m => m.UsuarioSolicitante)
                .Include(m => m.UsuarioAprobador)
                .Where(m => m.BienId == bienId)
                .OrderByDescending(m => m.FechaSolicitud)
                .ToListAsync();

            if (!User.HasClaim("Permiso", "Historial.VerTodos"))
            {
                int? userDept = null;
                if (int.TryParse(deptClaim, out int d))
                    userDept = d;
                else
                    return Forbid();

                var bien = await _context.Bienes.FindAsync(bienId);
                if (bien == null || bien.DependenciaID != userDept)
                    return Forbid();
            }

            return View(movimientos);
        }

        [HttpGet]
        [PermissionAuthorize("Actas.Imprimir")] 
        public async Task<IActionResult> Acta(int movimientoId)
        {
            var movimiento = await _context.Movements
                .Include(m => m.Bien)
                .Include(m => m.OriginDepartment)
                .Include(m => m.DestinationDepartment)
                .Include(m => m.UsuarioSolicitante)
                .Include(m => m.UsuarioAprobador)
                .FirstOrDefaultAsync(m => m.Id == movimientoId && m.Estado == "Aprobado");

            if (movimiento == null) return NotFound();

            var deptClaim = User.FindFirst("DepartmentId")?.Value;
            if (!User.HasClaim("Permiso", "Historial.VerTodos"))
            {
                int? userDept = null;
                if (int.TryParse(deptClaim, out int d))
                    userDept = d;
                else
                    return Forbid();

                if (movimiento.Bien != null && movimiento.Bien.DependenciaID != userDept)
                    return Forbid();
            }

            return View(movimiento);
        }
    }
}