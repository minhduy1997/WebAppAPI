namespace WebAppApi.Models;

/// <summary>Built-in application roles.</summary>
public static class AppRoles
{
    public const string Admin = "Admin";

    /// <summary>Internal staff (formerly "User").</summary>
    public const string Staff = "Staff";

    /// <summary>End-customer accounts (self-register from customer web).</summary>
    public const string Customer = "Customer";

    public static readonly string[] All = [Admin, Staff, Customer];

    /// <summary>Legacy role name before rename to Staff.</summary>
    public const string LegacyUser = "User";
}
