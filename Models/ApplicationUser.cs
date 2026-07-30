using Microsoft.AspNetCore.Identity;

namespace WebAppApi.Models;

public class ApplicationUser : IdentityUser
{
    public string? FullName { get; set; }

    /// <summary>Relative path to avatar, e.g. /uploads/avatars/photo_guid.jpg</summary>
    public string? AvatarUrl { get; set; }

    /// <summary>When false, user cannot log in.</summary>
    public bool IsActive { get; set; } = true;
}
