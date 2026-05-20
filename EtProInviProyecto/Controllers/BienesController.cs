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
    public class BienesController : Controller
    {
        private readonly AppDbContext _context;

        public BienesController(AppDbContext context)
        {
            _context = context;
        }

        private async Task<List<int>> ObtenerDepartamentosVisiblesAsync(string deptClaim)
        {
            if (int.TryParse(deptClaim, out int deptId))
            {
                var ids = await _context.Departments
                    .Where(d => d.ID == deptId || d.ParentDepartmentID == deptId)
                    .Select(d => d.ID)
                    .ToListAsync();
                return ids;
            }
            return new List<int>();
        }

        private async Task<List<int>> ObtenerDepartamentosVisiblesIdsAsync(string deptClaim)
        {
            if (int.TryParse(deptClaim, out int deptId))
            {
                return await _context.Departments
                    .Where(d => d.ID == deptId || d.ParentDepartmentID == deptId)
                    .Select(d => d.ID)
                    .ToListAsync();
            }
            return new List<int>();
        }


        [Route("/api/Lista")]
        [HttpPost]
        [PermissionAuthorize("Bienes.VerPropios")]
        public async Task<IActionResult> Lista(
            int pagina = 1,
            int resultadosPorPagina = 10,
            string? codigo = null,
            string? descripcion = null,
            int? departamentoId = null,
            bool? activo = null,
            string? grupo = null,
            string? subgrupo = null,
            string? seccion = null,
            string? responsableId = null,
            DateTime? fechaDesde = null,
            DateTime? fechaHasta = null)
        {
            resultadosPorPagina = Math.Min(resultadosPorPagina, 20);
            pagina = Math.Max(pagina, 1);

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var deptClaim = User.FindFirst("DepartmentId")?.Value;

            var query = _context.Bienes.Where(b => b.Activo && b.Aprobado);

            List<int> deptosVisibles = new List<int>();
            if (!User.HasClaim("Permiso", "Bienes.VerTodos"))
            {
                deptosVisibles = await ObtenerDepartamentosVisiblesAsync(deptClaim);
                if (deptosVisibles.Any())
                    query = query.Where(b => deptosVisibles.Contains(b.DependenciaID));
                else
                    return Json(new { total = 0, items = Array.Empty<object>() });
            }
            else
            {
                deptosVisibles = await _context.Departments.Select(d => d.ID).ToListAsync();
            }

            if (!string.IsNullOrWhiteSpace(codigo))
                query = query.Where(b => b.NumeroIdentificacion.Contains(codigo));
            if (!string.IsNullOrWhiteSpace(descripcion))
                query = query.Where(b => b.Nombre.Contains(descripcion) || b.Marca.Contains(descripcion) || b.ObservacionesAdicionales.Contains(descripcion));
            if (!string.IsNullOrWhiteSpace(grupo))
                query = query.Where(b => b.Grupo.ToString().Contains(grupo));
            if (!string.IsNullOrWhiteSpace(subgrupo))
                query = query.Where(b => b.Subgrupo.Contains(subgrupo));
            if (!string.IsNullOrWhiteSpace(seccion))
                query = query.Where(b => b.Seccion.Contains(seccion));

            if (!string.IsNullOrWhiteSpace(responsableId))
            {
                var deptosDelResponsable = await _context.Departments
                    .Where(d => d.ManagerID == responsableId && deptosVisibles.Contains(d.ID))
                    .Select(d => d.ID)
                    .ToListAsync();
                if (deptosDelResponsable.Any())
                    query = query.Where(b => deptosDelResponsable.Contains(b.DependenciaID));
                else
                    query = query.Where(b => false);
            }

            if (departamentoId.HasValue)
            {
                if (!deptosVisibles.Contains(departamentoId.Value))
                    departamentoId = null; // ignorar si no es visible
                else
                    query = query.Where(b => b.DependenciaID == departamentoId.Value);
            }

            if (activo.HasValue)
                query = query.Where(b => b.Activo == activo.Value);

            if (fechaDesde.HasValue)
                query = query.Where(b => b.FechaRegistro >= fechaDesde.Value);
            if (fechaHasta.HasValue)
            {
                DateTime fechaHastaAjustada = fechaHasta.Value.Date.AddDays(1).AddSeconds(-1);
                query = query.Where(b => b.FechaRegistro <= fechaHastaAjustada);
            }

            int total = await query.CountAsync();
            var items = await query
                .OrderBy(b => b.ID)
                .Skip((pagina - 1) * resultadosPorPagina)
                .Take(resultadosPorPagina)
                .ToListAsync();

            return Json(new { total, items });
        }


        [PermissionAuthorize("Bienes.VerPropios")]
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var deptIdClaim = User.FindFirst("DepartmentId")?.Value;

            var query = _context.Bienes.Where(b => b.Activo && b.Aprobado);

            if (!User.HasClaim("Permiso", "Bienes.VerTodos"))
            {
                var deptosVisibles = await ObtenerDepartamentosVisiblesAsync(deptIdClaim);
                if (deptosVisibles.Any())
                    query = query.Where(b => deptosVisibles.Contains(b.DependenciaID));
                else
                    return Forbid();
            }

            var bienes = await query.ToListAsync();
            return View(bienes);
        }

        [HttpGet]
        [PermissionAuthorize("Bienes.VerPropios")]
        public async Task<IActionResult> ObtenerDatosBien(int id)
        {
            var bien = await _context.Bienes.FindAsync(id);
            if (bien == null)
                return NotFound();

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var deptClaim = User.FindFirst("DepartmentId")?.Value;

            if (!User.HasClaim("Permiso", "Bienes.VerTodos"))
            {
                var deptosVisibles = await ObtenerDepartamentosVisiblesAsync(deptClaim);
                if (!deptosVisibles.Contains(bien.DependenciaID))
                    return Forbid();
            }

            var departamento = await _context.Departments.FindAsync(bien.DependenciaID);

            var datos = new
            {
                bien.ID,
                bien.NumeroIdentificacion,
                bien.Nombre,
                bien.Marca,
                bien.Modelo,
                bien.Serial,
                bien.Color,
                bien.Material,
                bien.ValorUnitario,
                Departamento = departamento?.Name,
                Estado = bien.Activo ? "Activo" : "Inactivo"
            };

            return Json(datos);
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
                Aprobado = false,
                FechaRegistro = DateTime.UtcNow
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
            var deptIDClaim = User.FindFirst("DepartmentId")?.Value;

            bool verTodos = await _context.UserPermission
                .AnyAsync(up => up.UserID == userID && up.Permission.Name == "Bienes.VerTodos");

            if (!verTodos)
            {
                var deptosVisibles = await ObtenerDepartamentosVisiblesAsync(deptIDClaim);
                if (!deptosVisibles.Contains(bien.DependenciaID))
                    return Forbid();
            }

            var departamento = await _context.Departments.FindAsync(bien.DependenciaID);
            ViewBag.DepartamentoNombre = departamento?.Name ?? "Sin asignar";

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
                .AnyAsync(up => up.UserID == userId && up.Permission.Name == "Bienes.VerTodos");

            bool readOnly = false;

            if (!editarTodos)
            {
                if (int.TryParse(deptIdClaim, out int deptId))
                {
                    if (bien.DependenciaID != deptId)
                    {
                        var deptosVisibles = await ObtenerDepartamentosVisiblesAsync(deptIdClaim);
                        if (!deptosVisibles.Contains(bien.DependenciaID))
                            return Forbid(); 

                        readOnly = true;
                    }
                }
                else
                {
                    return Forbid();
                }
            }

            ViewBag.Departments = new SelectList(_context.Departments, "ID", "Name", bien.DependenciaID);
            ViewBag.ReadOnly = readOnly;

            return View(bien);
        }

        [HttpPost]
        [PermissionAuthorize("Bienes.Editar")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ID,NumeroIdentificacion,Nombre,Marca,Modelo,Serial,Color,Material,ObservacionesAdicionales,Grupo,Subgrupo,Seccion,DependenciaID,ValorUnitario")] BienMueble bienEditado)
        {
            if (id != bienEditado.ID) return NotFound();

            var bienOriginal = await _context.Bienes.AsNoTracking().FirstOrDefaultAsync(b => b.ID == id);
            if (bienOriginal == null || !bienOriginal.Activo) return NotFound();

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var deptIdClaim = User.FindFirst("DepartmentId")?.Value;

            bool editarTodos = await _context.UserPermission
                .AnyAsync(up => up.UserID == userId && up.Permission.Name == "Bienes.VerTodos");

            if (!editarTodos)
            {
                if (int.TryParse(deptIdClaim, out int deptId))
                {
                    if (bienOriginal.DependenciaID != deptId)
                        return Forbid();
                }
                else
                {
                    return Forbid();
                }
            }

            if (ModelState.IsValid)
            {
                try
                {

                    bienEditado.Activo = bienOriginal.Activo;
                    bienEditado.Aprobado = bienOriginal.Aprobado;

                    _context.Update(bienEditado);
                    await _context.SaveChangesAsync();

                    TempData["Success"] = "El bien ha sido actualizado correctamente.";
                    return Redirect("/Home/Bienes");
                }
                catch (DbUpdateConcurrencyException)
                {
                    return NotFound();
                }
            }

            ViewBag.Departments = new SelectList(_context.Departments, "ID", "Name", bienEditado.DependenciaID);
            return View(bienEditado);
        }

    }
}