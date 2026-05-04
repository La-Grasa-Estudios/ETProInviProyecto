using EtPro.Models;
using EtPro.Data;
using EtPro.Models;
using EtPro.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace EtPro.Controllers
{
    [Authorize]
    [PermissionAuthorize("Admin.Usuarios")]
    public class AdminController : Controller
    {
        private readonly AppDbContext _context;

        public AdminController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var usuarios = await _context.Users
                .Include(u => u.ActualUserPermissions)
                .ThenInclude(up => up.Permission)
                .ToListAsync();

            ViewBag.Departments = await _context.Departments.ToDictionaryAsync(d => d.ID, d => d.Name);
            return View(usuarios);
        }

        public IActionResult Create()
        {
            ViewBag.Departments = new SelectList(_context.Departments, "ID", "Name");
            return View(new CreateUserViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateUserViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Departments = new SelectList(_context.Departments, "ID", "Name");
                return View(model);
            }

            if (await _context.Users.AnyAsync(u => u.UserName == model.UserName))
            {
                ModelState.AddModelError(nameof(model.UserName), "El nombre de usuario ya está en uso.");
                ViewBag.Departments = new SelectList(_context.Departments, "ID", "Name");
                return View(model);
            }

            var user = new User
            {
                UserName = model.UserName,
                FullName = model.FullName,   
                PasswordHash = PasswordHashingService.HashPassword(model.Password),
                DepartmentID = model.DepartmentID
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Usuario '{user.UserName}' creado exitosamente.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(string id)
        {
            if (id == null) return NotFound();
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();

            ViewBag.Departments = new SelectList(_context.Departments, "ID", "Name", user.DepartmentID);
            return View(new EditUserViewModel
            {
                ID = user.ID,
                UserName = user.UserName,
                FullName = user.FullName,
                DepartmentID = user.DepartmentID
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditUserViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Departments = new SelectList(_context.Departments, "ID", "Name", model.DepartmentID);
                return View(model);
            }

            var user = await _context.Users.FindAsync(model.ID);
            if (user == null) return NotFound();

            if (await _context.Users.AnyAsync(u => u.UserName == model.UserName && u.ID != model.ID))
            {
                ModelState.AddModelError(nameof(model.UserName), "El nombre de usuario ya está en uso.");
                ViewBag.Departments = new SelectList(_context.Departments, "ID", "Name", model.DepartmentID);
                return View(model);
            }

            user.UserName = model.UserName;
            user.FullName = model.FullName;
            user.DepartmentID = model.DepartmentID;

            if (!string.IsNullOrEmpty(model.NewPassword))
                user.PasswordHash = PasswordHashingService.HashPassword(model.NewPassword);

            await _context.SaveChangesAsync();

            TempData["Success"] = "Usuario actualizado correctamente.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (id == currentUserId)
                return Forbid();

            var user = await _context.Users.FindAsync(id);
            if (user != null)
            {
                var departmentsAsManager = await _context.Departments
                    .Where(d => d.ManagerID == id).ToListAsync();
                foreach (var dep in departmentsAsManager)
                    dep.ManagerID = null;

                var departmentsAsCustodian = await _context.Departments
                    .Where(d => d.CustodianID == id).ToListAsync();
                foreach (var dep in departmentsAsCustodian)
                    dep.CustodianID = null;

                var permisos = _context.UserPermission.Where(up => up.UserID == id);
                _context.UserPermission.RemoveRange(permisos);

                _context.Users.Remove(user);
                await _context.SaveChangesAsync();
            }

            TempData["Success"] = "Usuario eliminado correctamente.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> ManagePermissions(string id)
        {
            var user = await _context.Users
                .Include(u => u.ActualUserPermissions)
                .FirstOrDefaultAsync(u => u.ID == id);
            if (user == null) return NotFound();

            var allPermissions = await _context.Permissions.OrderBy(p => p.Category).ToListAsync();
            var templates = await _context.TemplatePermissions.ToListAsync();

            var model = new ManagePermissionsViewModel
            {
                UserId = user.ID,
                UserName = user.UserName,
                Permissions = allPermissions.Select(p => new PermissionCheck
                {
                    PermissionId = p.ID,
                    Name = p.Name,
                    Category = p.Category,
                    Assigned = user.ActualUserPermissions.Any(up => up.PermissionID == p.ID)
                }).ToList(),
                Templates = templates.Select(t => new TemplateOption
                {
                    Id = t.Id,
                    Name = t.Name
                }).ToList()
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ManagePermissions(string userId, int[] selectedPermissions)
        {
            var user = await _context.Users.Include(u => u.ActualUserPermissions).FirstOrDefaultAsync(u => u.ID == userId);
            if (user == null) return NotFound();

            var currentPermissions = _context.UserPermission.Where(up => up.UserID == userId);
            _context.UserPermission.RemoveRange(currentPermissions);

            if (selectedPermissions != null)
            {
                foreach (var permId in selectedPermissions)
                {
                    _context.UserPermission.Add(new UserPermission
                    {
                        UserID = userId,
                        PermissionID = permId
                    });
                }
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = "Permisos actualizados correctamente.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApplyTemplate(string userId, int templateId)
        {
            var user = await _context.Users.Include(u => u.ActualUserPermissions).FirstOrDefaultAsync(u => u.ID == userId);
            if (user == null) return NotFound();

            var template = await _context.TemplatePermissions
                .Include(t => t.Details)
                .FirstOrDefaultAsync(t => t.Id == templateId);
            if (template == null) return NotFound();

            var currentPermissions = _context.UserPermission.Where(up => up.UserID == userId);
            _context.UserPermission.RemoveRange(currentPermissions);

            foreach (var detail in template.Details)
            {
                _context.UserPermission.Add(new UserPermission
                {
                    UserID = userId,
                    PermissionID = detail.PermissionID
                });
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = $"Plantilla '{template.Name}' aplicada correctamente.";
            return RedirectToAction(nameof(ManagePermissions), new { id = userId });
        }
    }
}