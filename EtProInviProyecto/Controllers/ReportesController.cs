using System.Security.Claims;
using EtPro.Data;
using EtPro.Models;
using ETPro.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace ETPro.Controllers
{
    [Authorize]
    public class ReportesController : Controller
    {
        private readonly AppDbContext _context;
        private readonly PdfService _pdfService;

        public ReportesController(AppDbContext context, PdfService pdfService)
        {
            _context = context;
            _pdfService = pdfService;
        }

        [HttpGet]
        [PermissionAuthorize("Reportes.VerPropios")]
        public IActionResult InventarioPorDepartamento()
        {
            ViewBag.Departments = new SelectList(_context.Departments, "ID", "Name");
            ViewBag.Modo = "vista";
            return View();
        }

        [HttpPost]
        [PermissionAuthorize("Reportes.VerPropios")]
        public async Task<IActionResult> InventarioPorDepartamento(int? departamentoId, string modo = "vista")
        {
            var query = _context.Bienes.Where(b => b.Activo && b.Aprobado);
            var deptClaim = User.FindFirst("DepartmentId")?.Value;
            List<int> deptosVisibles = new List<int>();

            if (!User.HasClaim("Permiso", "Bienes.VerTodos"))
            {
                deptosVisibles = await ObtenerDepartamentosVisiblesIdsAsync(deptClaim);
                if (deptosVisibles.Any())
                    query = query.Where(b => deptosVisibles.Contains(b.DependenciaID));
                else
                    query = query.Where(b => false);
            }
            else
            {
                deptosVisibles = await _context.Departments.Select(d => d.ID).ToListAsync();
                if (departamentoId.HasValue && deptosVisibles.Contains(departamentoId.Value))
                    query = query.Where(b => b.DependenciaID == departamentoId.Value);
            }

            var bienes = await query.OrderBy(b => b.NumeroIdentificacion).ToListAsync();

            ViewBag.DepartamentoId = departamentoId;
            ViewBag.Modo = modo;

            ViewBag.Departments = new SelectList(_context.Departments, "ID", "Name", departamentoId);
            ViewBag.DepartmentNames = await _context.Departments.ToDictionaryAsync(d => d.ID, d => d.Name);
            ViewBag.LogoPath = ObtenerRutaLogo();
            ViewBag.LogoTrujilloPath = ObtenerRutaLogoTrujillo();
            ViewBag.FiltroAplicado = true;

            if (modo == "pdf")
            {
                string html = await RenderViewToString("_ReporteBM1PDF", bienes);
                var pdfBytes = _pdfService.GeneratePdf(html);
                return File(pdfBytes, "application/pdf", "inventario.pdf");
            }

            return View(bienes);
        }

        [HttpGet]
        [PermissionAuthorize("Reportes.VerPropios")]
        public IActionResult Movimientos()
        {
            var tipos = Enum.GetValues(typeof(MovementType))
                            .Cast<MovementType>()
                            .Select(v => new { Id = (int)v, Name = v.ToString() })
                            .ToList();
            ViewBag.Tipos = new SelectList(tipos, "Id", "Name");
            ViewBag.Modo = "vista";
            return View();
        }

        [HttpPost]
        [PermissionAuthorize("Reportes.VerPropios")]
        public async Task<IActionResult> Movimientos(MovementType? tipo, DateTime? desde, DateTime? hasta, string modo = "vista")
        {
            var query = _context.Movements
                .Include(m => m.Bien)
                .Include(m => m.OriginDepartment)
                .Include(m => m.DestinationDepartment)
                .Include(m => m.UsuarioSolicitante)
                .Include(m => m.UsuarioAprobador)
                .AsQueryable();

            if (!User.HasClaim("Permiso", "Historial.VerTodos"))
            {
                var deptClaim = User.FindFirst("DepartmentId")?.Value;
                var deptosVisibles = await ObtenerDepartamentosVisiblesIdsAsync(deptClaim);
                if (deptosVisibles.Any())
                    query = query.Where(m => deptosVisibles.Contains(m.Bien.DependenciaID));
                else
                    query = query.Where(m => false);
            }

            if (tipo.HasValue)
                query = query.Where(m => m.Type == tipo.Value);
            if (desde.HasValue)
                query = query.Where(m => m.FechaSolicitud >= desde.Value);
            if (hasta.HasValue)
            {
                DateTime fechaHastaAjustada = hasta.Value.Date.AddDays(1).AddSeconds(-1);
                query = query.Where(m => m.FechaSolicitud <= fechaHastaAjustada);
            }

            var movimientos = await query.OrderByDescending(m => m.FechaSolicitud).ToListAsync();

            ViewBag.Tipo = tipo;
            ViewBag.Desde = desde;
            ViewBag.Hasta = hasta;
            ViewBag.Modo = modo;

            var tipos = Enum.GetValues(typeof(MovementType))
                            .Cast<MovementType>()
                            .Select(v => new { Id = (int)v, Name = v.ToString() })
                            .ToList();
            ViewBag.Tipos = new SelectList(tipos, "Id", "Name", tipo.HasValue ? (int)tipo.Value : (int?)null);
            ViewBag.LogoPath = ObtenerRutaLogo();
            ViewBag.LogoTrujilloPath = ObtenerRutaLogoTrujillo();
            ViewBag.FiltroAplicado = true;
            if (modo == "pdf")
            {

                var incorporaciones = movimientos.Where(m => m.Type == MovementType.Incorporacion).ToList();
                var desincorporaciones = movimientos.Where(m => m.Type == MovementType.Desincorporacion).ToList();

                var modelo = new ReporteBM2ViewModel
                {
                    Mes = desde.HasValue ? desde.Value.ToString("MM/yyyy") : DateTime.Now.ToString("MM/yyyy"),
                    Incorporaciones = incorporaciones,
                    Desincorporaciones = desincorporaciones
                };

                string html = await RenderViewToString("_ReporteBM2PDF", modelo);
                var pdfBytes = _pdfService.GeneratePdf(html);
                return File(pdfBytes, "application/pdf", "movimientos.pdf");
            }

            return View(movimientos);
        }

        [HttpGet]
        [PermissionAuthorize("Reportes.VerPropios")]
        public IActionResult BienesDesincorporados()
        {
            ViewBag.Modo = "vista";
            return View();
        }

        [HttpPost]
        [PermissionAuthorize("Reportes.VerPropios")]
        public async Task<IActionResult> BienesDesincorporados(DateTime? desde, DateTime? hasta, string modo = "vista")
        {
            var query = _context.Bienes.Where(b => !b.Activo && b.Aprobado);

            if (!User.HasClaim("Permiso", "Bienes.VerTodos"))
            {
                var deptClaim = User.FindFirst("DepartmentId")?.Value;
                var deptosVisibles = await ObtenerDepartamentosVisiblesIdsAsync(deptClaim);
                if (deptosVisibles.Any())
                    query = query.Where(b => deptosVisibles.Contains(b.DependenciaID));
                else
                    query = query.Where(b => false);
            }

            var bienes = await query.OrderByDescending(b => b.ID).ToListAsync();

            ViewBag.Desde = desde;
            ViewBag.Hasta = hasta;
            ViewBag.Modo = modo;
            ViewBag.DepartmentNames = await _context.Departments.ToDictionaryAsync(d => d.ID, d => d.Name);
            ViewBag.LogoPath = ObtenerRutaLogo();
            ViewBag.LogoTrujilloPath = ObtenerRutaLogoTrujillo();


            if (modo == "pdf")
            {
                var modelo = new ReporteDesincorporadosViewModel { Bienes = bienes };
                string html = await RenderViewToString("_ReporteDesincorporadosPDF", modelo);
                var pdfBytes = _pdfService.GeneratePdf(html);
                return File(pdfBytes, "application/pdf", "desincorporados.pdf");
            }

            return View(bienes);
        }

        [HttpGet]
        [PermissionAuthorize("Reportes.VerPropios")]
        public async Task<IActionResult> EstadoInventario()
        {
            var modelo = await CalcularEstadoInventario();
            return View(modelo);
        }

        [HttpPost]
        [PermissionAuthorize("Reportes.VerPropios")]
        public async Task<IActionResult> EstadoInventario(string modo = "vista")
        {
            var modelo = await CalcularEstadoInventario();
            ViewBag.LogoPath = ObtenerRutaLogo();
            ViewBag.LogoTrujilloPath = ObtenerRutaLogoTrujillo();

            if (modo == "pdf")
            {
                string html = await RenderViewToString("_ReporteBM4PDF", modelo);
                var pdfBytes = _pdfService.GeneratePdf(html);
                return File(pdfBytes, "application/pdf", "estado_inventario.pdf");
            }

            return View(modelo);
        }

        private async Task<ReporteEstadoInventarioViewModel> CalcularEstadoInventario()
        {
            var deptClaim = User.FindFirst("DepartmentId")?.Value;
            IQueryable<BienMueble> bienesVisibles = _context.Bienes.Where(b => b.Activo && b.Aprobado);

            if (!User.HasClaim("Permiso", "Bienes.VerTodos"))
            {
                var deptosVisiblesIds = await ObtenerDepartamentosVisiblesIdsAsync(deptClaim);
                if (deptosVisiblesIds.Any())
                    bienesVisibles = bienesVisibles.Where(b => deptosVisiblesIds.Contains(b.DependenciaID));
                else
                    bienesVisibles = bienesVisibles.Where(b => false);
            }

            int total = await bienesVisibles.CountAsync();
            int activos = total;
            int desincorporados = await _context.Bienes.CountAsync(b => !b.Activo && b.Aprobado);
            int movimientos = await _context.Movements.CountAsync(m => m.Type == MovementType.Traspaso);

            return new ReporteEstadoInventarioViewModel
            {
                TotalBienes = total,
                TotalActivos = activos,
                TotalMantenimiento = movimientos,
                TotalDesincorporados = desincorporados
            };
        }

        private async Task<string> RenderViewToString(string viewName, object model)
        {
            ViewData.Model = model;
            using var writer = new StringWriter();

            var viewEngine = HttpContext.RequestServices.GetService(typeof(Microsoft.AspNetCore.Mvc.ViewEngines.ICompositeViewEngine))
                             as Microsoft.AspNetCore.Mvc.ViewEngines.ICompositeViewEngine;

            var viewResult = viewEngine.FindView(ControllerContext, viewName, false);
            if (!viewResult.Success)
                throw new InvalidOperationException($"La vista '{viewName}' no se encontró.");

            var viewContext = new ViewContext(
                ControllerContext,
                viewResult.View,
                ViewData,
                TempData,
                writer,
                new Microsoft.AspNetCore.Mvc.ViewFeatures.HtmlHelperOptions()
            );

            await viewResult.View.RenderAsync(viewContext);
            return writer.ToString();
        }

        private string ObtenerRutaLogo()
        {
            var logoPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "imagenes", "logo.jpg");
            return "file:///" + logoPath.Replace("\\", "/");
        }

        private string ObtenerRutaLogoTrujillo()
        {
            var logoPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "imagenes", "trujillo.png");
            return "file:///" + logoPath.Replace("\\", "/");
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
    }
}