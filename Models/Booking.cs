using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebAppApi.Models;

public class Booking
{
    public Guid Id { get; set; }

    [Required]
    [MaxLength(30)]
    public string BookingCode { get; set; } = string.Empty;

    /// <summary>FK to CustomerProfiles.Id</summary>
    public Guid CustomerId { get; set; }

    public Guid VehicleModelId { get; set; }

    public Guid? VehicleId { get; set; }

    /// <summary>Location entity not created yet — store id only.</summary>
    public Guid PickupLocationId { get; set; }

    public Guid ReturnLocationId { get; set; }

    public DateTime PickupAt { get; set; }

    public DateTime ExpectedReturnAt { get; set; }

    public DateTime? ActualReturnAt { get; set; }

    public BookingStatus Status { get; set; } = BookingStatus.DRAFT;

    [Column(TypeName = "decimal(18,2)")]
    public decimal QuotedAmount { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal DepositRequired { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal DepositPaid { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal DiscountAmount { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal DeliveryFee { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? FinalAmount { get; set; }

    [Required]
    [MaxLength(3)]
    public string CurrencyCode { get; set; } = "VND";

    [MaxLength(1000)]
    public string? CustomerNote { get; set; }

    [MaxLength(1000)]
    public string? InternalNote { get; set; }

    public DateTime? ExpiredAt { get; set; }

    public DateTime? ConfirmedAt { get; set; }

    public DateTime? CancelledAt { get; set; }

    [MaxLength(500)]
    public string? CancellationReason { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [MaxLength(450)]
    public string? CreatedBy { get; set; }

    public DateTime? UpdatedAt { get; set; }

    [MaxLength(450)]
    public string? UpdatedBy { get; set; }

    [ForeignKey(nameof(CustomerId))]
    public CustomerProfile? Customer { get; set; }

    [ForeignKey(nameof(VehicleModelId))]
    public VehicleModel? VehicleModel { get; set; }

    [ForeignKey(nameof(VehicleId))]
    public Vehicle? Vehicle { get; set; }
}
