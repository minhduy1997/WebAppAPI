using System.ComponentModel.DataAnnotations;

namespace WebAppApi.Models;

// ---- Auth DTOs (existing) ----

public class RegisterRequest
{
    [Required]
    [MaxLength(150)]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [MaxLength(150)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required]
    [MinLength(6)]
    public string Password { get; set; } = string.Empty;

    [Required]
    [MinLength(6)]
    public string ConfirmPassword { get; set; } = string.Empty;
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
    public Guid? CustomerProfileId { get; set; }
    public string? CustomerCode { get; set; }
    public string? VerificationStatus { get; set; }
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

// ---- Vehicles (physical units) DTOs ----

public class VehicleDto
{
    public Guid Id { get; set; }
    public Guid VehicleModelId { get; set; }
    public string VehicleModelCode { get; set; } = string.Empty;
    public string VehicleModelName { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string LicensePlate { get; set; } = string.Empty;
    public string? VinNumber { get; set; }
    public string? EngineNumber { get; set; }
    public string? Color { get; set; }
    public short? ManufactureYear { get; set; }
    public DateOnly? RegistrationDate { get; set; }
    public decimal CurrentOdometer { get; set; }
    public byte? FuelLevel { get; set; }
    public Guid? LocationId { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateOnly? RegistrationExpiredAt { get; set; }
    public DateOnly? InsuranceExpiredAt { get; set; }
    public DateOnly? MaintenanceDueAt { get; set; }
    public decimal? MaintenanceDueOdometer { get; set; }
    public DateOnly? PurchaseDate { get; set; }
    public decimal? PurchasePrice { get; set; }
    public decimal? RentalDailyPriceOverride { get; set; }
    public string? Note { get; set; }
    public bool IsActive { get; set; }
    public IList<VehicleImageDto> Images { get; set; } = new List<VehicleImageDto>();
}

public class VehicleImageDto
{
    public Guid Id { get; set; }
    public string Url { get; set; } = string.Empty;
    public string? OriginalFileName { get; set; }
    public int SortOrder { get; set; }
    public bool IsPrimary { get; set; }
}

public class VehicleRequest
{
    [Required]
    public Guid VehicleModelId { get; set; }

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

    public DateOnly? RegistrationDate { get; set; }

    [Required]
    public decimal CurrentOdometer { get; set; }

    public byte? FuelLevel { get; set; }

    public Guid? LocationId { get; set; }

    [Required]
    public string Status { get; set; } = "AVAILABLE";

    public DateOnly? RegistrationExpiredAt { get; set; }

    public DateOnly? InsuranceExpiredAt { get; set; }

    public DateOnly? MaintenanceDueAt { get; set; }

    public decimal? MaintenanceDueOdometer { get; set; }

    public DateOnly? PurchaseDate { get; set; }

    public decimal? PurchasePrice { get; set; }

    public decimal? RentalDailyPriceOverride { get; set; }

    [MaxLength(1000)]
    public string? Note { get; set; }

    public bool IsActive { get; set; } = true;
}

public class VehicleLookupsDto
{
    public IList<VehicleModelLookupItemDto> Models { get; set; } = new List<VehicleModelLookupItemDto>();
    public IList<string> Statuses { get; set; } = new List<string>();
}

public class VehicleModelLookupItemDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public string ModelName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
}

// ---- Customer profile DTOs ----

public class CustomerProfileDto
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string CustomerCode { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public DateOnly? DateOfBirth { get; set; }
    public string? Gender { get; set; }
    public string PhoneNumber { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? AddressLine { get; set; }
    public string? Ward { get; set; }
    public string? Province { get; set; }
    public string VerificationStatus { get; set; } = string.Empty;
    public DateTime? VerifiedAt { get; set; }
    public bool IsBlacklisted { get; set; }
    public string? BlacklistReason { get; set; }
    public string? Note { get; set; }
    public IList<CustomerDocumentDto> Documents { get; set; } = new List<CustomerDocumentDto>();
}

public class CustomerDocumentDto
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public string CustomerCode { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string DocumentType { get; set; } = string.Empty;
    public string? DocumentNumber { get; set; }
    public string? FrontImageUrl { get; set; }
    public string? BackImageUrl { get; set; }
    public DateOnly? IssuedDate { get; set; }
    public DateOnly? ExpiredAt { get; set; }
    public string VerificationStatus { get; set; } = string.Empty;
    public string? RejectionReason { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class UpsertCustomerDocumentRequest
{
    [MaxLength(50)]
    public string? DocumentNumber { get; set; }

    public DateOnly? IssuedDate { get; set; }

    public DateOnly? ExpiredAt { get; set; }
}

public class ReviewCustomerDocumentRequest
{
    [Required]
    public string Status { get; set; } = string.Empty; // VERIFIED | REJECTED

    [MaxLength(500)]
    public string? RejectionReason { get; set; }
}

public class UpdateCustomerBasicRequest
{
    [Required]
    [MaxLength(150)]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    public string PhoneNumber { get; set; } = string.Empty;

    public DateOnly? DateOfBirth { get; set; }

    public string? Gender { get; set; }

    [MaxLength(300)]
    public string? AddressLine { get; set; }

    [MaxLength(100)]
    public string? Ward { get; set; }

    [MaxLength(100)]
    public string? Province { get; set; }
}

public class SubmitCustomerDocumentsRequest
{
    /// <summary>Optional profile fields submitted together with documents.</summary>
    public DateOnly? DateOfBirth { get; set; }
    public string? Gender { get; set; }

    [MaxLength(300)]
    public string? AddressLine { get; set; }

    [MaxLength(100)]
    public string? Ward { get; set; }

    [MaxLength(100)]
    public string? Province { get; set; }
}

public class ReviewCustomerVerificationRequest
{
    [Required]
    public string Status { get; set; } = string.Empty; // VERIFIED | REJECTED

    [MaxLength(1000)]
    public string? Note { get; set; }
}

public class SetCustomerBlacklistRequest
{
    public bool IsBlacklisted { get; set; }

    [MaxLength(500)]
    public string? BlacklistReason { get; set; }
}

// ---- Booking DTOs ----

public class BookingDto
{
    public Guid Id { get; set; }
    public string BookingCode { get; set; } = string.Empty;
    public Guid CustomerId { get; set; }
    public string CustomerCode { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public Guid VehicleModelId { get; set; }
    public string VehicleModelCode { get; set; } = string.Empty;
    public string VehicleModelName { get; set; } = string.Empty;
    public Guid? VehicleId { get; set; }
    public string? VehicleCode { get; set; }
    public string? LicensePlate { get; set; }
    public Guid PickupLocationId { get; set; }
    public Guid ReturnLocationId { get; set; }
    public DateTime PickupAt { get; set; }
    public DateTime ExpectedReturnAt { get; set; }
    public DateTime? ActualReturnAt { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal QuotedAmount { get; set; }
    public decimal DepositRequired { get; set; }
    public decimal DepositPaid { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal DeliveryFee { get; set; }
    public decimal? FinalAmount { get; set; }
    public string CurrencyCode { get; set; } = "VND";
    public string? CustomerNote { get; set; }
    public string? InternalNote { get; set; }
    public DateTime? ExpiredAt { get; set; }
    public DateTime? ConfirmedAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public string? CancellationReason { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateBookingRequest
{
    [Required]
    public Guid VehicleModelId { get; set; }

    [Required]
    public Guid PickupLocationId { get; set; }

    [Required]
    public Guid ReturnLocationId { get; set; }

    [Required]
    public DateTime PickupAt { get; set; }

    [Required]
    public DateTime ExpectedReturnAt { get; set; }

    public decimal? DeliveryFee { get; set; }

    public decimal? DiscountAmount { get; set; }

    [MaxLength(1000)]
    public string? CustomerNote { get; set; }

    /// <summary>If true, create as PENDING_DEPOSIT (requires VERIFIED). Otherwise DRAFT.</summary>
    public bool Submit { get; set; } = true;
}

public class CancelBookingRequest
{
    [MaxLength(500)]
    public string? CancellationReason { get; set; }
}

public class AssignVehicleRequest
{
    [Required]
    public Guid VehicleId { get; set; }
}

public class UpdateBookingStatusRequest
{
    [Required]
    public string Status { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? InternalNote { get; set; }

    public decimal? DepositPaid { get; set; }

    public decimal? FinalAmount { get; set; }

    public DateTime? ActualReturnAt { get; set; }
}

public class BookingLookupsDto
{
    public IList<string> Statuses { get; set; } = new List<string>();
    public IList<string> VerificationStatuses { get; set; } = new List<string>();
    public IList<string> Genders { get; set; } = new List<string>();
    public IList<string> DocumentTypes { get; set; } = new List<string>();
}

// ---- Public catalog (customer website) DTOs ----

public class PublicCategoryDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

/// <summary>Card/list fields for published vehicle models.</summary>
public class PublicVehicleModelSummaryDto
{
    public Guid Id { get; set; }
    public string Brand { get; set; } = string.Empty;
    public string ModelName { get; set; } = string.Empty;
    public string? VariantName { get; set; }
    public short ManufactureYear { get; set; }
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public byte SeatCount { get; set; }
    public string TransmissionType { get; set; } = string.Empty;
    public string FuelType { get; set; } = string.Empty;
    public decimal BaseDailyPrice { get; set; }
    public decimal DepositAmount { get; set; }
    public string? ThumbnailUrl { get; set; }
}

/// <summary>Full customer-facing detail (no admin/ops fields).</summary>
public class PublicVehicleModelDetailDto : PublicVehicleModelSummaryDto
{
    public byte? DoorCount { get; set; }
    public decimal? EngineCapacity { get; set; }
    public string? Drivetrain { get; set; }
    public byte? LuggageCapacity { get; set; }
    public decimal? FuelConsumption { get; set; }
    public decimal? WeekendDailyPrice { get; set; }
    public decimal? HolidayDailyPrice { get; set; }
    public int? IncludedKmPerDay { get; set; }
    public decimal? ExtraKmPrice { get; set; }
    public decimal? LateHourPrice { get; set; }
    public string? Description { get; set; }
}

public class PublicCatalogLookupsDto
{
    public IList<PublicCategoryDto> Categories { get; set; } = new List<PublicCategoryDto>();
    public IList<string> Brands { get; set; } = new List<string>();
    public IList<string> TransmissionTypes { get; set; } = new List<string>();
    public IList<string> FuelTypes { get; set; } = new List<string>();
    public IList<string> Drivetrains { get; set; } = new List<string>();
}


