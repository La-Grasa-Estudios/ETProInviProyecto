using ETPro.Data;
using ETPro.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EtPro.Models;
using System.Security.Claims;

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
        var deptIdClaim = User.FindFirst("DepartamentoId")?.Value; 

        var query = _context.Bienes.AsQueryable();


        bool hasVerTodos = await _context.UserPermission
            .AnyAsync(up => up.UserID == userId && up.Permission.Name == "Bienes.VerTodos");

        if (!hasVerTodos)
        {

            if (int.TryParse(deptIdClaim, out int deptId))
                query = query.Where(b => b.DependenciaID == deptId);
            else
                return Forbid(); 
        }

        var bienes = await query.ToListAsync();
        return View(bienes);
    }

    [PermissionAuthorize("Bienes.Crear")]
    public IActionResult Create() => View();

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
        return View(bien);
    }

    
}