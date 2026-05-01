using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ETPro.Models;
using EtPro.Models;
using EtPro.Models;

namespace ETPro.Data
{
    public class AppDbContext : DbContext
    {
        public DbSet<User> Users { get; set; }
        public DbSet<BienMueble> Bienes { get; set; }
        public DbSet<Permission> Permissions { get; set; }
        public DbSet<TemplatePermission> TemplatePermissions { get; set; }
        public DbSet<TemplatePermissionDetails> TemplatePermissionDetails { get; set; }
        public DbSet<UserPermission> UserPermission { get; set; }

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
        }
    }
}
