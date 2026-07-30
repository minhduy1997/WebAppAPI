using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using WebAppApi.Models;

namespace WebAppApi.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<AppPage> AppPages => Set<AppPage>();
    public DbSet<RolePagePermission> RolePagePermissions => Set<RolePagePermission>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<AppPage>(e =>
        {
            e.HasIndex(x => x.Path).IsUnique();
        });

        builder.Entity<RolePagePermission>(e =>
        {
            e.HasIndex(x => new { x.RoleId, x.PageId }).IsUnique();
            e.HasOne(x => x.Page)
                .WithMany(p => p.RolePermissions)
                .HasForeignKey(x => x.PageId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Role)
                .WithMany()
                .HasForeignKey(x => x.RoleId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
