using System.ComponentModel.DataAnnotations;

namespace WebAppApi.Models;

// ---- Auth DTOs (existing) ----

public class RegisterRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MinLength(6)]
    public string Password { get; set; } = string.Empty;

    public string? FullName { get; set; }
}

public class LoginRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}

public class AuthResponse
{
    public string Token { get; set; } = string.Empty;
    public DateTime Expiration { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? FullName { get; set; }
    public string? AvatarUrl { get; set; }
    public IList<string> Roles { get; set; } = new List<string>();
    public IList<string> AllowedPages { get; set; } = new List<string>();
}

public class AssignRoleRequest
{
    [Required]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Role { get; set; } = string.Empty;
}

public class ChangePasswordRequest
{
    [Required]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required]
    [MinLength(6)]
    public string NewPassword { get; set; } = string.Empty;
}

public class UpdateProfileRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string FullName { get; set; } = string.Empty;
}

// ---- Admin User DTOs ----

public class UserListItemDto
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? FullName { get; set; }
    public string? AvatarUrl { get; set; }
    public bool IsActive { get; set; }
    public IList<string> Roles { get; set; } = new List<string>();
}

public class CreateUserRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MinLength(6)]
    public string Password { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? FullName { get; set; }

    public IList<string> Roles { get; set; } = new List<string>();
}

public class UpdateUserRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? FullName { get; set; }

    public bool IsActive { get; set; } = true;

    public IList<string> Roles { get; set; } = new List<string>();
}

public class ResetPasswordRequest
{
    [Required]
    [MinLength(6)]
    public string NewPassword { get; set; } = string.Empty;
}

public class SetUserActiveRequest
{
    public bool IsActive { get; set; }
}

// ---- Page DTOs ----

public class PageDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; }
}

public class CreatePageRequest
{
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
}

public class UpdatePageRequest : CreatePageRequest
{
}

// ---- Role DTOs ----

public class RoleDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int UserCount { get; set; }
}

public class CreateRoleRequest
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
}

public class UpdateRoleRequest
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
}

public class SetUserRolesRequest
{
    public IList<string> Roles { get; set; } = new List<string>();
}

// ---- Permission DTOs ----

public class RolePagePermissionDto
{
    public string RoleId { get; set; } = string.Empty;
    public string RoleName { get; set; } = string.Empty;
    public IList<int> PageIds { get; set; } = new List<int>();
}

public class SetRolePagesRequest
{
    [Required]
    public string RoleId { get; set; } = string.Empty;

    public IList<int> PageIds { get; set; } = new List<int>();
}

// ---- Vehicle Categories DTOs ----

public class VehicleCategoriesDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public class CreateVehicleCategoriesRequest
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(250)]
    public string Description { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
}

public class UpdateVehicleCategoryRequest
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

// ---- Vehicle Models DTOs ----

public class VehicleModelDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public string ModelName { get; set; } = string.Empty;
    public string? VariantName { get; set; }
    public short ManufactureYear { get; set; }
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public byte SeatCount { get; set; }
    public byte? DoorCount { get; set; }
    public string TransmissionType { get; set; } = string.Empty;
    public string FuelType { get; set; } = string.Empty;
    public decimal? EngineCapacity { get; set; }
    public string? Drivetrain { get; set; }
    public byte? LuggageCapacity { get; set; }
    public decimal? FuelConsumption { get; set; }
    public decimal BaseDailyPrice { get; set; }
    public decimal? WeekendDailyPrice { get; set; }
    public decimal? HolidayDailyPrice { get; set; }
    public decimal DepositAmount { get; set; }
    public int? IncludedKmPerDay { get; set; }
    public decimal? ExtraKmPrice { get; set; }
    public decimal? LateHourPrice { get; set; }
    public string? ThumbnailUrl { get; set; }
    public string? Description { get; set; }
    public bool IsPublished { get; set; }
    public bool IsActive { get; set; }
    public int SortOrder { get; set; }
}

public class VehicleModelRequest
{
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

    [Required]
    public short ManufactureYear { get; set; }

    [Required]
    public int CategoryId { get; set; }

    [Required]
    public byte SeatCount { get; set; }

    public byte? DoorCount { get; set; }

    [Required]
    public string TransmissionType { get; set; } = string.Empty;

    [Required]
    public string FuelType { get; set; } = string.Empty;

    public decimal? EngineCapacity { get; set; }

    public string? Drivetrain { get; set; }

    public byte? LuggageCapacity { get; set; }

    public decimal? FuelConsumption { get; set; }

    [Required]
    public decimal BaseDailyPrice { get; set; }

    public decimal? WeekendDailyPrice { get; set; }

    public decimal? HolidayDailyPrice { get; set; }

    [Required]
    public decimal DepositAmount { get; set; }

    public int? IncludedKmPerDay { get; set; }

    public decimal? ExtraKmPrice { get; set; }

    public decimal? LateHourPrice { get; set; }

    [MaxLength(500)]
    public string? ThumbnailUrl { get; set; }

    public string? Description { get; set; }

    public bool IsPublished { get; set; } = true;

    public bool IsActive { get; set; } = true;

    public int SortOrder { get; set; }
}

public class VehicleModelLookupsDto
{
    public IList<VehicleCategoriesDto> Categories { get; set; } = new List<VehicleCategoriesDto>();
    public IList<string> TransmissionTypes { get; set; } = new List<string>();
    public IList<string> FuelTypes { get; set; } = new List<string>();
    public IList<string> Drivetrains { get; set; } = new List<string>();
}

