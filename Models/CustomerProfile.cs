using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebAppApi.Models;

/// <summary>Extended profile for Customer-role accounts.</summary>
public class CustomerProfile
{
    public Guid Id { get; set; }

    /// <summary>FK to AspNetUsers.Id (string).</summary>
    [Required]
    [MaxLength(450)]
    public string UserId { get; set; } = string.Empty;

    [Required]
    [MaxLength(30)]
    public string CustomerCode { get; set; } = string.Empty;

    [Required]
    [MaxLength(150)]
    public string FullName { get; set; } = string.Empty;

    [Column(TypeName = "date")]
    public DateOnly? DateOfBirth { get; set; }

    public CustomerGender? Gender { get; set; }

    [Required]
    [MaxLength(20)]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required]
    [MaxLength(150)]
    public string Email { get; set; } = string.Empty;

    [MaxLength(300)]
    public string? AddressLine { get; set; }

    [MaxLength(100)]
    public string? Ward { get; set; }

    [MaxLength(100)]
    public string? Province { get; set; }

    [MaxLength(30)]
    public string? IdentityNumber { get; set; }

    [Column(TypeName = "date")]
    public DateOnly? IdentityIssuedDate { get; set; }

    [MaxLength(150)]
    public string? IdentityIssuedPlace { get; set; }

    [MaxLength(500)]
    public string? IdentityFrontImageUrl { get; set; }

    [MaxLength(500)]
    public string? IdentityBackImageUrl { get; set; }

    [MaxLength(50)]
    public string? DriverLicenseNumber { get; set; }

    [MaxLength(20)]
    public string? DriverLicenseClass { get; set; }

    [Column(TypeName = "date")]
    public DateOnly? DriverLicenseExpiredAt { get; set; }

    [MaxLength(500)]
    public string? DriverLicenseFrontImageUrl { get; set; }

    public CustomerVerificationStatus VerificationStatus { get; set; } =
        CustomerVerificationStatus.NOT_SUBMITTED;

    public DateTime? VerifiedAt { get; set; }

    /// <summary>Staff AspNetUsers.Id who verified the profile.</summary>
    [MaxLength(450)]
    public string? VerifiedBy { get; set; }

    public bool IsBlacklisted { get; set; }

    [MaxLength(500)]
    public string? BlacklistReason { get; set; }

    [MaxLength(1000)]
    public string? Note { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    [ForeignKey(nameof(UserId))]
    public ApplicationUser? User { get; set; }

    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();

    public ICollection<CustomerDocument> Documents { get; set; } = new List<CustomerDocument>();
}
