using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebAppApi.Models;

/// <summary>Image belonging to a physical vehicle unit.</summary>
public class VehicleImage
{
    public Guid Id { get; set; }

    public Guid VehicleId { get; set; }

    /// <summary>Public URL path, e.g. /uploads/vehicles/{vehicleId}/photo_guid.jpg</summary>
    [Required]
    [MaxLength(500)]
    public string Url { get; set; } = string.Empty;

    [MaxLength(255)]
    public string? OriginalFileName { get; set; }

    public int SortOrder { get; set; }

    public bool IsPrimary { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Guid? CreatedBy { get; set; }

    [ForeignKey(nameof(VehicleId))]
    public Vehicle? Vehicle { get; set; }
}
