using EtPro.Models;
using ETPro.Data;
using ETPro.Models;
using ETPro.Services;
using Microsoft.EntityFrameworkCore;

namespace ETPro.Data
{
    public static class DbInitializer
    {
        private static readonly (string Name, string Description, string Category)[] PermissionList = new[]
        {
            ("Bienes.VerTodos", "Ver todos los bienes de la empresa", "Bienes"),
            ("Bienes.VerPropios", "Ver solo bienes de su departamento", "Bienes"),
            ("Bienes.Crear", "Registrar nuevos bienes", "Bienes"),
            ("Bienes.Editar", "Editar información de bienes", "Bienes"),
            ("Bienes.Desincorporar", "Solicitar desincorporación de bienes", "Bienes"),
            ("Movimientos.SolicitarTraspaso", "Solicitar traspaso de bienes", "Movimientos"),
            ("Movimientos.AprobarTraspaso", "Aprobar o rechazar solicitudes de traspaso", "Movimientos"),
            ("Movimientos.AprobarDesincorporacion", "Aprobar o rechazar desincorporaciones", "Movimientos"),
            ("Historial.VerTodos", "Ver historial de cualquier bien", "Historial"),
            ("Historial.VerPropios", "Ver historial de bienes de su departamento", "Historial"),
            ("Reportes.VerTodos", "Generar cualquier reporte", "Reportes"),
            ("Reportes.VerPropios", "Generar reportes de su departamento", "Reportes"),
            ("Admin.Usuarios", "Gestionar usuarios y asignar permisos", "Administración"),
            ("Admin.Plantillas", "Crear y editar plantillas de permisos", "Administración"),
            ("Admin.Departamentos", "Gestionar departamentos", "Administración"),
            ("Etiquetas.Generar", "Generar e imprimir etiquetas", "Etiquetas"),
            ("Etiquetas.Ignorar", "Ignorar alerta de etiquetas desactualizadas", "Etiquetas"),
            ("Inventario.Verificar", "Recibir notificación para verificar inventario", "Inventario"),
            ("Inventario.Confirmar", "Confirmar verificación de inventario", "Inventario")
        };

        public static async Task InitializeAsync(AppDbContext context)
        {
            context.Database.EnsureCreated();

            foreach (var (name, desc, cat) in PermissionList)
            {
                if (!context.Permissions.Any(p => p.Name == name))
                {
                    context.Permissions.Add(new Permission { Name = name, Description = desc, Category = cat });
                }
            }
            await context.SaveChangesAsync();

            if (!context.TemplatePermissions.Any())
            {
                var all = await context.Permissions.ToListAsync();

                var superadmin = new TemplatePermission { Name = "Superadmin", Description = "Acceso total", Editable = false };
                superadmin.Details = all.Select(p => new TemplatePermissionDetails { PermissionInstance = p }).ToList();
                context.TemplatePermissions.Add(superadmin);

                var adminPermissions = new[]
                {
                    "Bienes.VerTodos", "Bienes.Crear", "Bienes.Editar",
                    "Movimientos.AprobarTraspaso", "Movimientos.AprobarDesincorporacion",
                    "Historial.VerTodos", "Reportes.VerTodos",
                    "Etiquetas.Generar", "Etiquetas.Ignorar", "Inventario.Confirmar"
                };
                var admin = new TemplatePermission { Name = "Administrador de Bienes", Description = "Gestión total de bienes y aprobaciones" };
                admin.Details = all.Where(p => adminPermissions.Contains(p.Name))
                                   .Select(p => new TemplatePermissionDetails { PermissionInstance = p }).ToList();
                context.TemplatePermissions.Add(admin);

                var departmentAdminPermissions = new[]
                {
                    "Bienes.VerPropios", "Bienes.Editar", "Bienes.Desincorporar",
                    "Movimientos.SolicitarTraspaso", "Historial.VerPropios",
                    "Reportes.VerPropios", "Inventario.Verificar", "Inventario.Confirmar"
                };
                var depAdmin = new TemplatePermission { Name = "Responsable de Departamento", Description = "Gestiona bienes de su área" };
                depAdmin.Details = all.Where(p => departmentAdminPermissions.Contains(p.Name))
                                      .Select(p => new TemplatePermissionDetails { PermissionInstance = p }).ToList();
                context.TemplatePermissions.Add(depAdmin);

                var watcherPermissions = new[] {
                    "Bienes.VerPropios", "Bienes.Desincorporar",
                    "Movimientos.SolicitarTraspaso", "Historial.VerPropios",
                    "Reportes.VerPropios", "Inventario.Verificar"
                };
                var watcher = new TemplatePermission { Name = "Custodio", Description = "Supervisa el inventario de su departamento" };
                watcher.Details = all.Where(p => watcherPermissions.Contains(p.Name))
                                     .Select(p => new TemplatePermissionDetails { PermissionInstance = p }).ToList();
                context.TemplatePermissions.Add(watcher);

                var consultPermissions = new[] {
                    "Bienes.VerTodos", "Historial.VerTodos", "Reportes.VerTodos"
                };
                var consult = new TemplatePermission { Name = "Consulta / Auditoría", Description = "Solo lectura de toda la información" };
                consult.Details = all.Where(p => consultPermissions.Contains(p.Name))
                                     .Select(p => new TemplatePermissionDetails { PermissionInstance = p }).ToList();
                context.TemplatePermissions.Add(consult);

                await context.SaveChangesAsync();

                if (!context.Users.Any(u => u.UserName == "superadmin"))
                {
                    var superuser = new User
                    {
                        ID = Guid.NewGuid().ToString(),
                        UserName = "superadmin",
                        PasswordHash = PasswordHashingService.HashPassword("superadmin"),
                        DepartmentID = null
                    };  
                    context.Users.Add(superuser);
                    await context.SaveChangesAsync();

                    var allSuperPermission = await context.Permissions.ToListAsync();
                    foreach (var permission in allSuperPermission)
                    {
                        context.UserPermission.Add(new UserPermission { UserID = superuser.ID, PermissionID = permission.ID });
                    }
                    await context.SaveChangesAsync();
                }
            }
        }
    }
}