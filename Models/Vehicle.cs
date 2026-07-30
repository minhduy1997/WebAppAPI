using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebAppApi.Models;

/// <summary>A specific physical vehicle unit in the fleet.</summary>
public class Vehicle
{
    public Guid Id { get; set; }

    public Guid VehicleModelId { get; set; }

    [Required]
    [MaxLength(50)]
    public string Code { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    public string LicensePlate { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? VinNumber { get; set; }

    [MaxLength(50)]
    public string? EngineNumber { get; set; }

    [MaxLength(50)]
    public string? Color { get; set; }

    public short? ManufactureYear { get; set; }

    [Column(TypeName = "date")]
    public DateOnly? RegistrationDate { get; set; }

    [Column(TypeName = "decimal(12,1)")]
    public decimal CurrentOdometer { get; set; }

    public byte? FuelLevel { get; set; }

    /// <summary>Optional FK to a future Locations table.</summary>
    public Guid? LocationId { get; set; }

    public VehicleStatus Status { get; set; } = VehicleStatus.AVAILABLE;

    [Column(TypeName = "date")]
    public DateOnly? RegistrationExpiredAt { get; set; }

    [Column(TypeName = "date")]
    public DateOnly? InsuranceExpiredAt { get; set; }

    [Column(TypeName = "date")]
    public DateOnly? MaintenanceDueAt { get; set; }

    [Column(TypeName = "decimal(12,1)")]
    public decimal? MaintenanceDueOdometer { get; set; }

    [Column(TypeName = "date")]
    public DateOnly? PurchaseDate { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? PurchasePrice { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? RentalDailyPriceOverride { get; set; }

    [MaxLength(1000)]
    public string? Note { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Guid? CreatedBy { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public Guid? UpdatedBy { get; set; }

    [ForeignKey(nameof(VehicleModelId))]
    public VehicleModel? VehicleModel { get; set; }

    public ICollection<VehicleImage> Images { get; set; } = new List<VehicleImage>();
}
