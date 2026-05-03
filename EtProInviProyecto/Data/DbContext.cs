using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using EtPro.Models;

namespace EtPro.Data
{
    public class AppDbContext : DbContext
    {
        public DbSet<User> Users { get; set; }
        public DbSet<BienMueble> Bienes { get; set; }
        public DbSet<Permission> Permissions { get; set; }
        public DbSet<TemplatePermission> TemplatePermissions { get; set; }
        public DbSet<TemplatePermissionDetails> TemplatePermissionDetails { get; set; }
        public DbSet<UserPermission> UserPermission { get; set; }
        public DbSet<Department> Departments { get; set; }
        public DbSet<Movement> Movements { get; set; }


        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder builder)
        {

            base.OnModelCreating(builder);

            builder.Entity<User>(e =>
            {
                e.HasKey(u => u.ID);
                e.Property(u => u.ID).HasMaxLength(450);
            });

            builder.Entity<UserPermission>()
                .HasKey(up => new { up.UserID, up.PermissionID });

            builder.Entity<UserPermission>()
                .HasOne(up => up.UserInstance)
                .WithMany(u => u.ActualUserPermissions)
                .HasForeignKey(up => up.UserID);

            builder.Entity<UserPermission>()
                .HasOne(up => up.Permission)
                .WithMany(p => p.UserPermissions)
                .HasForeignKey(up => up.PermissionID);

            builder.Entity<TemplatePermissionDetails>()
                .HasKey(pp => new { pp.TemplateID, pp.PermissionID });

            builder.Entity<TemplatePermissionDetails>()
                .HasOne(pp => pp.Template)
                .WithMany(p => p.Details)
                .HasForeignKey(pp => pp.TemplateID);

            builder.Entity<TemplatePermissionDetails>()
                .HasOne(pp => pp.PermissionInstance)
                .WithMany(p => p.TemplatePermissionInfo)
                .HasForeignKey(pp => pp.PermissionID);

            builder.Entity<BienMueble>(entity =>
            {
                entity.ToTable("BienesMuebles");
                entity.Property(b => b.NumeroIdentificacion).IsRequired().HasMaxLength(50);

            });
            builder.Entity<BienMueble>().Property(b => b.DependenciaID).HasColumnName("DependenciaID");

            builder.Entity<Department>()
                .HasOne(d => d.Manager)
                .WithMany()
                .HasForeignKey(d => d.ManagerID).IsRequired(false);

            builder.Entity<Department>()
                .HasOne(d => d.Custodian)
                .WithMany()
                .HasForeignKey(d => d.CustodianID)
                .IsRequired(false);

            builder.Entity<Movement>()
                .HasOne(m => m.Bien)
                .WithMany()
                .HasForeignKey(m => m.BienId)
                .OnDelete(DeleteBehavior.Restrict);  

            builder.Entity<Movement>()
                .HasOne(m => m.OriginDepartment)
                .WithMany()
                .HasForeignKey(m => m.OriginDepartmentId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Movement>()
                .HasOne(m => m.DestinationDepartment)
                .WithMany()
                .HasForeignKey(m => m.DestinationDepartmentId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Movement>()
                .HasOne(m => m.UsuarioSolicitante)
                .WithMany()
                .HasForeignKey(m => m.UsuarioSolicitanteId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Movement>()
                .HasOne(m => m.UsuarioAprobador)
                .WithMany()
                .HasForeignKey(m => m.UsuarioAprobadorId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<TemplatePermissionDetails>(entity =>
            {
                entity.HasOne(d => d.Template)
                      .WithMany(t => t.Details)
                      .HasForeignKey(d => d.TemplateID);

                entity.HasOne(d => d.PermissionInstance)
                      .WithMany(p => p.TemplatePermissionInfo)
                      .HasForeignKey(d => d.PermissionID);
            });
        }
    }
}
