using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace WebAppApi.Models;

public class AppPage
{
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string Path { get; set; } = string.Empty;

    [MaxLength(300)]
    public string? Description { get; set; }

    public int SortOrder { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<RolePagePermission> RolePermissions { get; set; } = new List<RolePagePermission>();
}

public class RolePagePermission
{
    public int Id { get; set; }

    [Required]
    public string RoleId { get; set; } = string.Empty;

    public int PageId { get; set; }

    [ForeignKey(nameof(RoleId))]
    public IdentityRole? Role { get; set; }

    [ForeignKey(nameof(PageId))]
    public AppPage? Page { get; set; }
}
