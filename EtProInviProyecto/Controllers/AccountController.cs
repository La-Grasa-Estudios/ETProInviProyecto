using EtPro.Models;
using EtPro.Data;
using EtPro.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace EtPro.Controllers
{
    [AllowAnonymous]
    public class AccountController : Controller
    {
        private readonly AppDbContext _context;

        public AccountController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Login(string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (!ModelState.IsValid)
                return View(model);

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.UserName == model.Username);

            if (user == null || !PasswordHashingService.VerifyPassword(model.Password, user.PasswordHash))
            {
                ModelState.AddModelError(string.Empty, "Nombre de usuario o contraseña incorrectos.");
                return View(model);
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.ID),
                new Claim(ClaimTypes.Name, user.UserName),
                new Claim("FullName", user.FullName),
                new Claim("DepartmentId", user.DepartmentID?.ToString() ?? "")
            };

            var permissions = await _context.UserPermission
            .Where(up => up.UserID == user.ID)
            .Select(up => up.Permission.Name)
            .ToListAsync();

            claims.AddRange(permissions.Select(p => new Claim("Permiso", p)));

            string role = "Usuario";
            if (permissions.Contains("Admin.Usuarios"))
                role = "Superadmin";
            else if (permissions.Contains("Movimientos.AprobarTraspaso"))
                role = "Administrador de Bienes";
            else if (permissions.Contains("Bienes.Editar"))
                role = "Gerente / Responsable";
            else if (permissions.Contains("Inventario.Verificar"))
                role = "Custodio";
            else if (permissions.Contains("Reportes.VerTodos"))
                role = "Auditor";

            claims.Add(new Claim("Role", role));

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                new AuthenticationProperties
                {
                    IsPersistent = model.RememberMe,
                    ExpiresUtc = model.RememberMe ? DateTime.UtcNow.AddDays(30) : DateTime.UtcNow.AddHours(2)
                });

            return RedirectToLocal(returnUrl);
        }

        [HttpGet]
        [Authorize]
        public IActionResult ChangePassword()
        {
            return View(new ChangePasswordViewModel());
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return NotFound();

            if (!PasswordHashingService.VerifyPassword(model.CurrentPassword, user.PasswordHash))
            {
                ModelState.AddModelError(nameof(model.CurrentPassword), "La contraseña actual es incorrecta.");
                return View(model);
            }

            user.PasswordHash = PasswordHashingService.HashPassword(model.NewPassword);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Contraseña cambiada exitosamente.";
            return RedirectToAction(nameof(ChangePassword));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }


        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }

        private IActionResult RedirectToLocal(string returnUrl)
        {
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);
            return RedirectToAction("Index", "Home");
        }
    }
}