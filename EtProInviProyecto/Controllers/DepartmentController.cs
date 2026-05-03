using EtPro.Models;
using EtPro.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace EtPro.Controllers
{
    [Authorize]
    public class DepartmentController : Controller
    {
        private readonly AppDbContext _context;

        public DepartmentController(AppDbContext context)
        {
            _context = context;
        }

        [PermissionAuthorize("Admin.Departamentos")]
        public async Task<IActionResult> Index()
        {
            var departments = await _context.Departments
                .Include(d => d.Manager)
                .Include(d => d.Custodian)
                .ToListAsync();
            return View(departments);
        }

        [PermissionAuthorize("Admin.Departamentos")]
        public IActionResult Create()
        {
            var managerIds = _context.UserPermission
                .Where(up => up.Permission.Name == "Bienes.VerPropios")
                .Select(up => up.UserID)
                .Distinct();

            var managers = _context.Users
                .Where(u => managerIds.Contains(u.ID))
                .ToList();

            var custodianIds = _context.UserPermission
                .Where(up => up.Permission.Name == "Inventario.Verificar")
                .Select(up => up.UserID)
                .Distinct();

            var custodians = _context.Users
                .Where(u => custodianIds.Contains(u.ID))
                .ToList();

            ViewBag.Managers = new SelectList(managers, "ID", "UserName");
            ViewBag.Custodians = new SelectList(custodians, "ID", "UserName");

            return View();
        }

        [HttpPost]
        [PermissionAuthorize("Admin.Departamentos")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Department department)
        {
            if (ModelState.IsValid)
            {
                _context.Departments.Add(department);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(department);
        }

        [PermissionAuthorize("Admin.Departamentos")]
        public async Task<IActionResult> Edit(int? ID)
        {
            if (ID == null) return NotFound();
            var department = await _context.Departments.FindAsync(ID);
            if (department == null) return NotFound();

            var managerIds = _context.UserPermission
                .Where(up => up.Permission.Name == "Bienes.VerPropios")
                .Select(up => up.UserID)
                .Distinct();
            var managers = await _context.Users
                .Where(u => managerIds.Contains(u.ID))
                .ToListAsync();

            var custodianIds = _context.UserPermission
                .Where(up => up.Permission.Name == "Inventario.Verificar")
                .Select(up => up.UserID)
                .Distinct();
            var custodians = await _context.Users
                .Where(u => custodianIds.Contains(u.ID))
                .ToListAsync();

            ViewBag.Managers = new SelectList(managers, "ID", "UserName");
            ViewBag.Custodians = new SelectList(custodians, "ID", "UserName");

            return View(department);
        }

        [HttpPost]
        [PermissionAuthorize("Admin.Departamentos")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int ID, Department department)
        {
            if (ID != department.ID) return NotFound();
            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(department);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Departments.Any(d => d.ID == ID))
                        return NotFound();
                    throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(department);
        }

        [PermissionAuthorize("Admin.Departamentos")]
        public async Task<IActionResult> Delete(int? ID)
        {
            if (ID == null) return NotFound();
            var department = await _context.Departments.FindAsync(ID);
            if (department == null) return NotFound();
            return View(department);
        }

        [HttpPost, ActionName("Delete")]
        [PermissionAuthorize("Admin.Departamentos")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int ID)
        {
            var department = await _context.Departments.FindAsync(ID);
            if (department != null)
            {
                _context.Departments.Remove(department);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}