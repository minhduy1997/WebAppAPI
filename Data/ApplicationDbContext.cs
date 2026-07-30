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
    public DbSet<Vehicle> Vehicles => Set<Vehicle>();
    public DbSet<VehicleImage> VehicleImages => Set<VehicleImage>();
    public DbSet<CustomerProfile> CustomerProfiles => Set<CustomerProfile>();
    public DbSet<Booking> Bookings => Set<Booking>();

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

        builder.Entity<Vehicle>(e =>
        {
            e.ToTable("Vehicles");
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Code).IsUnique();
            e.HasIndex(x => x.LicensePlate).IsUnique();
            e.Property(x => x.Code).IsRequired().HasMaxLength(50);
            e.Property(x => x.LicensePlate).IsRequired().HasMaxLength(20);
            e.Property(x => x.VinNumber).HasMaxLength(50);
            e.Property(x => x.EngineNumber).HasMaxLength(50);
            e.Property(x => x.Color).HasMaxLength(50);
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(30);
            e.Property(x => x.Note).HasMaxLength(1000);
            e.Property(x => x.CurrentOdometer).HasPrecision(12, 1);
            e.Property(x => x.MaintenanceDueOdometer).HasPrecision(12, 1);
            e.Property(x => x.PurchasePrice).HasPrecision(18, 2);
            e.Property(x => x.RentalDailyPriceOverride).HasPrecision(18, 2);

            e.HasOne(x => x.VehicleModel)
                .WithMany()
                .HasForeignKey(x => x.VehicleModelId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<VehicleImage>(e =>
        {
            e.ToTable("VehicleImages");
            e.HasKey(x => x.Id);
            e.Property(x => x.Url).IsRequired().HasMaxLength(500);
            e.Property(x => x.OriginalFileName).HasMaxLength(255);
            e.HasIndex(x => new { x.VehicleId, x.SortOrder });

            e.HasOne(x => x.Vehicle)
                .WithMany(v => v.Images)
                .HasForeignKey(x => x.VehicleId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<CustomerProfile>(e =>
        {
            e.ToTable("CustomerProfiles");
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.UserId).IsUnique();
            e.HasIndex(x => x.CustomerCode).IsUnique();
            e.HasIndex(x => x.PhoneNumber);
            e.HasIndex(x => x.Email);
            e.Property(x => x.UserId).IsRequired().HasMaxLength(450);
            e.Property(x => x.CustomerCode).IsRequired().HasMaxLength(30);
            e.Property(x => x.FullName).IsRequired().HasMaxLength(150);
            e.Property(x => x.PhoneNumber).IsRequired().HasMaxLength(20);
            e.Property(x => x.Email).IsRequired().HasMaxLength(150);
            e.Property(x => x.AddressLine).HasMaxLength(300);
            e.Property(x => x.Ward).HasMaxLength(100);
            e.Property(x => x.Province).HasMaxLength(100);
            e.Property(x => x.IdentityNumber).HasMaxLength(30);
            e.Property(x => x.IdentityIssuedPlace).HasMaxLength(150);
            e.Property(x => x.IdentityFrontImageUrl).HasMaxLength(500);
            e.Property(x => x.IdentityBackImageUrl).HasMaxLength(500);
            e.Property(x => x.DriverLicenseNumber).HasMaxLength(50);
            e.Property(x => x.DriverLicenseClass).HasMaxLength(20);
            e.Property(x => x.DriverLicenseFrontImageUrl).HasMaxLength(500);
            e.Property(x => x.BlacklistReason).HasMaxLength(500);
            e.Property(x => x.Note).HasMaxLength(1000);
            e.Property(x => x.VerifiedBy).HasMaxLength(450);
            e.Property(x => x.Gender).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.VerificationStatus).HasConversion<string>().HasMaxLength(30);

            e.HasOne(x => x.User)
                .WithOne()
                .HasForeignKey<CustomerProfile>(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Booking>(e =>
        {
            e.ToTable("Bookings");
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.BookingCode).IsUnique();
            e.HasIndex(x => x.CustomerId);
            e.HasIndex(x => x.Status);
            e.HasIndex(x => x.PickupAt);
            e.Property(x => x.BookingCode).IsRequired().HasMaxLength(30);
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(30);
            e.Property(x => x.QuotedAmount).HasPrecision(18, 2);
            e.Property(x => x.DepositRequired).HasPrecision(18, 2);
            e.Property(x => x.DepositPaid).HasPrecision(18, 2);
            e.Property(x => x.DiscountAmount).HasPrecision(18, 2);
            e.Property(x => x.DeliveryFee).HasPrecision(18, 2);
            e.Property(x => x.FinalAmount).HasPrecision(18, 2);
            e.Property(x => x.CurrencyCode).IsRequired().HasMaxLength(3).IsFixedLength();
            e.Property(x => x.CustomerNote).HasMaxLength(1000);
            e.Property(x => x.InternalNote).HasMaxLength(1000);
            e.Property(x => x.CancellationReason).HasMaxLength(500);
            e.Property(x => x.CreatedBy).HasMaxLength(450);
            e.Property(x => x.UpdatedBy).HasMaxLength(450);

            e.HasOne(x => x.Customer)
                .WithMany(c => c.Bookings)
                .HasForeignKey(x => x.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.VehicleModel)
                .WithMany()
                .HasForeignKey(x => x.VehicleModelId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.Vehicle)
                .WithMany()
                .HasForeignKey(x => x.VehicleId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
