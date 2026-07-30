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
    public DbSet<VehicleCategories> VehicleCategories => Set<VehicleCategories>();
    public DbSet<VehicleModel> VehicleModels => Set<VehicleModel>();

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

        builder.Entity<VehicleCategories>(e =>
        {
            e.HasIndex(x => x.Id).IsUnique();
            e.Property(x => x.Name).IsRequired().HasMaxLength(50);
            e.Property(x => x.Description).IsRequired().HasMaxLength(250);
        });

        builder.Entity<VehicleModel>(e =>
        {
            e.ToTable("VehicleModels");
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Code).IsUnique();
            e.Property(x => x.Code).IsRequired().HasMaxLength(50);
            e.Property(x => x.Brand).IsRequired().HasMaxLength(100);
            e.Property(x => x.ModelName).IsRequired().HasMaxLength(100);
            e.Property(x => x.VariantName).HasMaxLength(100);
            e.Property(x => x.TransmissionType).HasConversion<string>().HasMaxLength(30);
            e.Property(x => x.FuelType).HasConversion<string>().HasMaxLength(30);
            e.Property(x => x.Drivetrain)
                .HasConversion(
                    v => v.HasValue ? (v.Value == Drivetrain.FourWD ? "4WD" : v.Value.ToString()) : null,
                    v => string.IsNullOrEmpty(v)
                        ? null
                        : v == "4WD"
                            ? Drivetrain.FourWD
                            : Enum.Parse<Drivetrain>(v))
                .HasMaxLength(20);
            e.Property(x => x.ThumbnailUrl).HasMaxLength(500);
            e.Property(x => x.Description).HasColumnType("nvarchar(max)");
            e.Property(x => x.EngineCapacity).HasPrecision(4, 1);
            e.Property(x => x.FuelConsumption).HasPrecision(5, 2);
            e.Property(x => x.BaseDailyPrice).HasPrecision(18, 2);
            e.Property(x => x.WeekendDailyPrice).HasPrecision(18, 2);
            e.Property(x => x.HolidayDailyPrice).HasPrecision(18, 2);
            e.Property(x => x.DepositAmount).HasPrecision(18, 2);
            e.Property(x => x.ExtraKmPrice).HasPrecision(18, 2);
            e.Property(x => x.LateHourPrice).HasPrecision(18, 2);

            e.HasOne(x => x.Category)
                .WithMany(c => c.VehicleModels)
                .HasForeignKey(x => x.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
