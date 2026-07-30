using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebAppApi.Models;

public class CustomerDocument
{
    public Guid Id { get; set; }

    public Guid CustomerId { get; set; }

    public CustomerDocumentType DocumentType { get; set; }

    [MaxLength(50)]
    public string? DocumentNumber { get; set; }

    [MaxLength(500)]
    public string? FrontImageUrl { get; set; }

    [MaxLength(500)]
    public string? BackImageUrl { get; set; }

    [Column(TypeName = "date")]
    public DateOnly? IssuedDate { get; set; }

    [Column(TypeName = "date")]
    public DateOnly? ExpiredAt { get; set; }

    public CustomerVerificationStatus VerificationStatus { get; set; } =
        CustomerVerificationStatus.NOT_SUBMITTED;

    [MaxLength(500)]
    public string? RejectionReason { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    [ForeignKey(nameof(CustomerId))]
    public CustomerProfile? Customer { get; set; }
}
