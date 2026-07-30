using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebAppApi.Models;

public class VehicleModel
{
    public Guid Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string Code { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Brand { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string ModelName { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? VariantName { get; set; }

    public short ManufactureYear { get; set; }

    /// <summary>FK to VehicleCategories (vehicle type: Sedan, SUV, MPV...).</summary>
    public int CategoryId { get; set; }

    public byte SeatCount { get; set; }

    public byte? DoorCount { get; set; }

    public TransmissionType TransmissionType { get; set; }

    public FuelType FuelType { get; set; }

    [Column(TypeName = "decimal(4,1)")]
    public decimal? EngineCapacity { get; set; }

    public Drivetrain? Drivetrain { get; set; }

    public byte? LuggageCapacity { get; set; }

    [Column(TypeName = "decimal(5,2)")]
    public decimal? FuelConsumption { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal BaseDailyPrice { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? WeekendDailyPrice { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? HolidayDailyPrice { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal DepositAmount { get; set; }

    public int? IncludedKmPerDay { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? ExtraKmPrice { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? LateHourPrice { get; set; }

    [MaxLength(500)]
    public string? ThumbnailUrl { get; set; }

    public string? Description { get; set; }

    public bool IsPublished { get; set; } = true;

    public bool IsActive { get; set; } = true;

    public int SortOrder { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Guid? CreatedBy { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public Guid? UpdatedBy { get; set; }

    [ForeignKey(nameof(CategoryId))]
    public VehicleCategories? Category { get; set; }
}
