using EtPro.Data;
using EtPro.Models;
using ETPro.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Security.Claims;

namespace ETPro.Controllers
{
    [Authorize]
    public class EtiquetasController : Controller
    {
        private readonly AppDbContext _context;
        private readonly QrService _qrService;
        private readonly PdfService _pdfService;

        public EtiquetasController(AppDbContext context, QrService qrService, PdfService pdfService)
        {
            _context = context;
            _qrService = qrService;
            _pdfService = pdfService;
        }

        private string ObtenerIpLocal()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            var ip = host.AddressList.FirstOrDefault(ip => ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork);
            return ip?.ToString() ?? "localhost";
        }

        // GET: Vista de selección de bienes
        [HttpGet]
        [PermissionAuthorize("Etiquetas.Generar")]
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var deptClaim = User.FindFirst("DepartmentId")?.Value;

            var query = _context.Bienes.Where(b => b.Activo && b.Aprobado);

            if (!User.HasClaim("Permiso", "Bienes.VerTodos"))
            {
                if (int.TryParse(deptClaim, out int deptId))
                {
                    var deptosVisibles = await _context.Departments
                        .Where(d => d.ID == deptId || d.ParentDepartmentID == deptId)
                        .Select(d => d.ID)
                        .ToListAsync();
                    query = query.Where(b => deptosVisibles.Contains(b.DependenciaID));
                }
                else
                {
                    query = query.Where(b => false);
                }
            }

            var bienes = await query.OrderBy(b => b.NumeroIdentificacion).ToListAsync();
            ViewBag.Departments = await _context.Departments.ToListAsync();

            if (TempData.ContainsKey("SelectedIds"))
                ViewBag.SelectedIds = (int[])TempData["SelectedIds"];
            else
                ViewBag.SelectedIds = new int[0];

            return View(bienes);
        }

        [HttpPost]
        [PermissionAuthorize("Etiquetas.Generar")]
        public IActionResult Index(int[] bienesIds)
        {
            TempData["SelectedIds"] = bienesIds ?? new int[0];
            return RedirectToAction(nameof(Index));
        }

        // POST: Generar vista previa
        [HttpPost]
        [PermissionAuthorize("Etiquetas.Generar")]
        public async Task<IActionResult> GenerarVistaPrevia(int[] bienesIds)
        {
            if (bienesIds == null || !bienesIds.Any())
                return RedirectToAction(nameof(Index));

            var bienes = await _context.Bienes
                .Where(b => bienesIds.Contains(b.ID))
                .ToListAsync();

            var departamentos = await _context.Departments.ToDictionaryAsync(d => d.ID, d => d.Name);

            string ip = ObtenerIpLocal();
            int puerto = Request.Host.Port ?? 5015;  
            string baseUrl = $"https://{ip}:{puerto}";

            var etiquetas = bienes.Select(b => new EtiquetaViewModel
            {
                BienId = b.ID,
                Grupo = b.Grupo.ToString(),
                Subgrupo = b.Subgrupo ?? "–",
                Codigo = b.NumeroIdentificacion,
                Descripcion = b.Nombre,
                Ubicacion = departamentos.ContainsKey(b.DependenciaID) ? departamentos[b.DependenciaID] : "Sin ubicación",
                Anio = b.FechaRegistro?.Year.ToString() ?? DateTime.Now.Year.ToString(),
                QrBase64 = _qrService.GenerateBase64Qr($"{baseUrl}/Bienes/Details/{b.ID}")
            }).ToList();

            return View("VistaPrevia", etiquetas);
        }

        // POST: Generar PDF
        [HttpPost]
        [PermissionAuthorize("Etiquetas.Generar")]
        public async Task<IActionResult> GenerarPDF(int[] bienesIds)
        {
            if (bienesIds == null || !bienesIds.Any())
                return RedirectToAction(nameof(Index));

            var bienes = await _context.Bienes
                .Where(b => bienesIds.Contains(b.ID))
                .ToListAsync();

            var departamentos = await _context.Departments.ToDictionaryAsync(d => d.ID, d => d.Name);

            string ip = ObtenerIpLocal();
            int puerto = Request.Host.Port ?? 5015;
            string baseUrl = $"https://{ip}:{puerto}";

            var etiquetas = bienes.Select(b => new EtiquetaViewModel
            {
                BienId = b.ID,
                Grupo = b.Grupo.ToString(),
                Subgrupo = b.Subgrupo ?? "–",
                Codigo = b.NumeroIdentificacion,
                Descripcion = b.Nombre,
                Ubicacion = departamentos.ContainsKey(b.DependenciaID) ? departamentos[b.DependenciaID] : "Sin ubicación",
                Anio = b.FechaRegistro?.Year.ToString() ?? DateTime.Now.Year.ToString(),
                QrBase64 = _qrService.GenerateBase64Qr($"{baseUrl}/Bienes/Details/{b.ID}")
            }).ToList();

            string html = await RenderViewToString("_EtiquetasPDF", etiquetas);
            byte[] pdfBytes = _pdfService.GeneratePdf(html);
            return File(pdfBytes, "application/pdf", $"etiquetas_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");
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
            var viewContext = new ViewContext(ControllerContext, viewResult.View, ViewData, TempData, writer,
                new Microsoft.AspNetCore.Mvc.ViewFeatures.HtmlHelperOptions());
            await viewResult.View.RenderAsync(viewContext);
            return writer.ToString();
        }
    }
}