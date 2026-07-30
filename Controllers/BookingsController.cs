using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebAppApi.Data;
using WebAppApi.Models;

namespace WebAppApi.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class BookingsController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public BookingsController(ApplicationDbContext db)
    {
        _db = db;
    }

    [HttpGet("mine")]
    [Authorize(Roles = AppRoles.Customer)]
    public async Task<ActionResult<IEnumerable<BookingDto>>> GetMine()
    {
        var profile = await GetCurrentCustomerProfileAsync();
        if (profile == null)
            return NotFound(new { message = "Customer profile not found" });

        var list = await QueryBookings()
            .Where(x => x.CustomerId == profile.Id)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();

        return Ok(list.Select(ToDto));
    }

    [HttpGet("{id:guid}")]
    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Staff},{AppRoles.Customer}")]
    public async Task<ActionResult<BookingDto>> GetById(Guid id)
    {
        var booking = await QueryBookings().FirstOrDefaultAsync(x => x.Id == id);
        if (booking == null)
            return NotFound(new { message = "Booking not found" });

        if (User.IsInRole(AppRoles.Customer))
        {
            var profile = await GetCurrentCustomerProfileAsync();
            if (profile == null || booking.CustomerId != profile.Id)
                return Forbid();
        }

        return Ok(ToDto(booking));
    }

    [HttpGet]
    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Staff}")]
    public async Task<ActionResult<IEnumerable<BookingDto>>> GetAll([FromQuery] string? status = null)
    {
        var query = QueryBookings();
        if (!string.IsNullOrWhiteSpace(status)
            && Enum.TryParse<BookingStatus>(status, true, out var parsed))
        {
            query = query.Where(x => x.Status == parsed);
        }

        var list = await query.OrderByDescending(x => x.CreatedAt).ToListAsync();
        return Ok(list.Select(ToDto));
    }

    /// <summary>
    /// Customer creates a booking. Submit=true requires VERIFIED profile and sets PENDING_DEPOSIT.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = AppRoles.Customer)]
    public async Task<ActionResult<BookingDto>> Create([FromBody] CreateBookingRequest request)
    {
        var profile = await GetCurrentCustomerProfileAsync();
        if (profile == null)
            return NotFound(new { message = "Customer profile not found" });

        if (profile.IsBlacklisted)
            return BadRequest(new { message = "Account is blacklisted and cannot create bookings" });

        var validation = ValidateSchedule(request.PickupAt, request.ExpectedReturnAt);
        if (validation != null)
            return validation;

        var model = await _db.VehicleModels.FirstOrDefaultAsync(x =>
            x.Id == request.VehicleModelId && x.IsActive && x.IsPublished);
        if (model == null)
            return BadRequest(new { message = "Invalid or unavailable vehicle model" });

        if (request.Submit)
        {
            var gate = EnsureVerifiedForBooking(profile);
            if (gate != null)
                return gate;
        }

        var days = Math.Max(1, (int)Math.Ceiling((request.ExpectedReturnAt - request.PickupAt).TotalDays));
        var quoted = model.BaseDailyPrice * days;
        var deliveryFee = request.DeliveryFee ?? 0;
        var discount = request.DiscountAmount ?? 0;
        if (deliveryFee < 0 || discount < 0)
            return BadRequest(new { message = "Delivery fee and discount must be >= 0" });

        var booking = new Booking
        {
            Id = Guid.NewGuid(),
            BookingCode = await GenerateBookingCodeAsync(),
            CustomerId = profile.Id,
            VehicleModelId = model.Id,
            PickupLocationId = request.PickupLocationId,
            ReturnLocationId = request.ReturnLocationId,
            PickupAt = request.PickupAt,
            ExpectedReturnAt = request.ExpectedReturnAt,
            Status = request.Submit ? BookingStatus.PENDING_DEPOSIT : BookingStatus.DRAFT,
            QuotedAmount = quoted,
            DepositRequired = model.DepositAmount,
            DepositPaid = 0,
            DiscountAmount = discount,
            DeliveryFee = deliveryFee,
            CurrencyCode = "VND",
            CustomerNote = string.IsNullOrWhiteSpace(request.CustomerNote) ? null : request.CustomerNote.Trim(),
            ExpiredAt = request.Submit ? DateTime.UtcNow.AddHours(24) : null,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = GetCurrentUserId(),
        };

        _db.Bookings.Add(booking);
        await _db.SaveChangesAsync();

        booking = await QueryBookings().FirstAsync(x => x.Id == booking.Id);
        return Ok(ToDto(booking));
    }

    [HttpPost("{id:guid}/submit")]
    [Authorize(Roles = AppRoles.Customer)]
    public async Task<ActionResult<BookingDto>> Submit(Guid id)
    {
        var profile = await GetCurrentCustomerProfileAsync();
        if (profile == null)
            return NotFound(new { message = "Customer profile not found" });

        var booking = await QueryBookings().FirstOrDefaultAsync(x => x.Id == id);
        if (booking == null)
            return NotFound(new { message = "Booking not found" });

        if (booking.CustomerId != profile.Id)
            return Forbid();

        if (booking.Status != BookingStatus.DRAFT)
            return BadRequest(new { message = "Only DRAFT bookings can be submitted" });

        var gate = EnsureVerifiedForBooking(profile);
        if (gate != null)
            return gate;

        booking.Status = BookingStatus.PENDING_DEPOSIT;
        booking.ExpiredAt = DateTime.UtcNow.AddHours(24);
        booking.UpdatedAt = DateTime.UtcNow;
        booking.UpdatedBy = GetCurrentUserId();
        await _db.SaveChangesAsync();

        return Ok(ToDto(booking));
    }

    [HttpPost("{id:guid}/cancel")]
    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Staff},{AppRoles.Customer}")]
    public async Task<ActionResult<BookingDto>> Cancel(Guid id, [FromBody] CancelBookingRequest? request)
    {
        var booking = await QueryBookings().FirstOrDefaultAsync(x => x.Id == id);
        if (booking == null)
            return NotFound(new { message = "Booking not found" });

        if (User.IsInRole(AppRoles.Customer))
        {
            var profile = await GetCurrentCustomerProfileAsync();
            if (profile == null || booking.CustomerId != profile.Id)
                return Forbid();

            if (booking.Status is BookingStatus.IN_PROGRESS or BookingStatus.COMPLETED
                or BookingStatus.CANCELLED or BookingStatus.EXPIRED)
            {
                return BadRequest(new { message = "Booking cannot be cancelled in current status" });
            }
        }

        if (booking.Status is BookingStatus.CANCELLED or BookingStatus.COMPLETED)
            return BadRequest(new { message = "Booking is already closed" });

        booking.Status = BookingStatus.CANCELLED;
        booking.CancelledAt = DateTime.UtcNow;
        booking.CancellationReason = string.IsNullOrWhiteSpace(request?.CancellationReason)
            ? null
            : request!.CancellationReason.Trim();
        booking.UpdatedAt = DateTime.UtcNow;
        booking.UpdatedBy = GetCurrentUserId();
        await _db.SaveChangesAsync();

        return Ok(ToDto(booking));
    }

    [HttpPut("{id:guid}/assign-vehicle")]
    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Staff}")]
    public async Task<ActionResult<BookingDto>> AssignVehicle(Guid id, [FromBody] AssignVehicleRequest request)
    {
        var booking = await QueryBookings().FirstOrDefaultAsync(x => x.Id == id);
        if (booking == null)
            return NotFound(new { message = "Booking not found" });

        if (booking.Status is BookingStatus.CANCELLED or BookingStatus.EXPIRED or BookingStatus.COMPLETED)
            return BadRequest(new { message = "Cannot assign vehicle to a closed booking" });

        var vehicle = await _db.Vehicles.FirstOrDefaultAsync(x =>
            x.Id == request.VehicleId && x.IsActive);
        if (vehicle == null)
            return BadRequest(new { message = "Invalid or inactive vehicle" });

        if (vehicle.VehicleModelId != booking.VehicleModelId)
            return BadRequest(new { message = "Vehicle does not match booking model" });

        if (vehicle.Status is not (VehicleStatus.AVAILABLE or VehicleStatus.RESERVED))
            return BadRequest(new { message = "Vehicle is not available for assignment" });

        booking.VehicleId = vehicle.Id;
        booking.Status = BookingStatus.VEHICLE_ASSIGNED;
        vehicle.Status = VehicleStatus.RESERVED;
        booking.UpdatedAt = DateTime.UtcNow;
        booking.UpdatedBy = GetCurrentUserId();
        await _db.SaveChangesAsync();

        booking = await QueryBookings().FirstAsync(x => x.Id == id);
        return Ok(ToDto(booking));
    }

    [HttpPut("{id:guid}/status")]
    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Staff}")]
    public async Task<ActionResult<BookingDto>> UpdateStatus(Guid id, [FromBody] UpdateBookingStatusRequest request)
    {
        var booking = await QueryBookings().FirstOrDefaultAsync(x => x.Id == id);
        if (booking == null)
            return NotFound(new { message = "Booking not found" });

        if (!Enum.TryParse<BookingStatus>(request.Status, true, out var status))
            return BadRequest(new { message = "Invalid booking status" });

        booking.Status = status;
        if (status == BookingStatus.CONFIRMED && booking.ConfirmedAt == null)
            booking.ConfirmedAt = DateTime.UtcNow;
        if (status == BookingStatus.CANCELLED && booking.CancelledAt == null)
            booking.CancelledAt = DateTime.UtcNow;
        if (status == BookingStatus.COMPLETED && booking.ActualReturnAt == null)
            booking.ActualReturnAt = request.ActualReturnAt ?? DateTime.UtcNow;
        else if (request.ActualReturnAt.HasValue)
            booking.ActualReturnAt = request.ActualReturnAt;

        if (request.DepositPaid.HasValue)
        {
            if (request.DepositPaid.Value < 0)
                return BadRequest(new { message = "Deposit paid must be >= 0" });
            booking.DepositPaid = request.DepositPaid.Value;
        }

        if (request.FinalAmount.HasValue)
        {
            if (request.FinalAmount.Value < 0)
                return BadRequest(new { message = "Final amount must be >= 0" });
            booking.FinalAmount = request.FinalAmount;
        }

        if (!string.IsNullOrWhiteSpace(request.InternalNote))
            booking.InternalNote = request.InternalNote.Trim();

        if (booking.Vehicle != null)
        {
            booking.Vehicle.Status = status switch
            {
                BookingStatus.IN_PROGRESS => VehicleStatus.RENTED,
                BookingStatus.READY_FOR_PICKUP => VehicleStatus.RESERVED,
                BookingStatus.VEHICLE_ASSIGNED => VehicleStatus.RESERVED,
                BookingStatus.COMPLETED => VehicleStatus.AVAILABLE,
                BookingStatus.CANCELLED => VehicleStatus.AVAILABLE,
                BookingStatus.EXPIRED => VehicleStatus.AVAILABLE,
                _ => booking.Vehicle.Status,
            };
        }

        booking.UpdatedAt = DateTime.UtcNow;
        booking.UpdatedBy = GetCurrentUserId();
        await _db.SaveChangesAsync();

        booking = await QueryBookings().FirstAsync(x => x.Id == id);
        return Ok(ToDto(booking));
    }

    private IQueryable<Booking> QueryBookings() =>
        _db.Bookings
            .Include(x => x.Customer)
            .Include(x => x.VehicleModel)
            .Include(x => x.Vehicle)
            .AsQueryable();

    private async Task<CustomerProfile?> GetCurrentCustomerProfileAsync()
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return null;

        return await _db.CustomerProfiles.FirstOrDefaultAsync(x => x.UserId == userId);
    }

    private string? GetCurrentUserId() =>
        User.FindFirstValue(ClaimTypes.NameIdentifier);

    private ActionResult? EnsureVerifiedForBooking(CustomerProfile profile)
    {
        if (profile.VerificationStatus == CustomerVerificationStatus.VERIFIED)
            return null;

        if (profile.VerificationStatus == CustomerVerificationStatus.NOT_SUBMITTED)
        {
            return BadRequest(new
            {
                message = "Please submit identity and driver license documents before booking",
                verificationStatus = profile.VerificationStatus.ToString(),
                required = true
            });
        }

        if (profile.VerificationStatus == CustomerVerificationStatus.PENDING)
        {
            return BadRequest(new
            {
                message = "Your documents are pending staff review. Please wait until verified before booking",
                verificationStatus = profile.VerificationStatus.ToString()
            });
        }

        return BadRequest(new
        {
            message = $"Cannot book with verification status {profile.VerificationStatus}",
            verificationStatus = profile.VerificationStatus.ToString()
        });
    }

    private ActionResult? ValidateSchedule(DateTime pickupAt, DateTime expectedReturnAt)
    {
        if (expectedReturnAt <= pickupAt)
            return BadRequest(new { message = "Expected return must be after pickup time" });

        if (pickupAt < DateTime.UtcNow.AddMinutes(-5))
            return BadRequest(new { message = "Pickup time cannot be in the past" });

        return null;
    }

    private async Task<string> GenerateBookingCodeAsync()
    {
        var prefix = $"BK{DateTime.UtcNow:yyMMdd}";
        var existing = await _db.Bookings
            .Where(x => x.BookingCode.StartsWith(prefix))
            .Select(x => x.BookingCode)
            .ToListAsync();

        var next = 1;
        foreach (var code in existing)
        {
            var suffix = code[prefix.Length..];
            if (int.TryParse(suffix, out var n) && n >= next)
                next = n + 1;
        }

        return $"{prefix}{next:D3}";
    }

    private static BookingDto ToDto(Booking b) => new()
    {
        Id = b.Id,
        BookingCode = b.BookingCode,
        CustomerId = b.CustomerId,
        CustomerCode = b.Customer?.CustomerCode ?? string.Empty,
        CustomerName = b.Customer?.FullName ?? string.Empty,
        VehicleModelId = b.VehicleModelId,
        VehicleModelCode = b.VehicleModel?.Code ?? string.Empty,
        VehicleModelName = b.VehicleModel == null
            ? string.Empty
            : string.IsNullOrWhiteSpace(b.VehicleModel.VariantName)
                ? $"{b.VehicleModel.Brand} {b.VehicleModel.ModelName}"
                : $"{b.VehicleModel.Brand} {b.VehicleModel.ModelName} {b.VehicleModel.VariantName}",
        VehicleId = b.VehicleId,
        VehicleCode = b.Vehicle?.Code,
        LicensePlate = b.Vehicle?.LicensePlate,
        PickupLocationId = b.PickupLocationId,
        ReturnLocationId = b.ReturnLocationId,
        PickupAt = b.PickupAt,
        ExpectedReturnAt = b.ExpectedReturnAt,
        ActualReturnAt = b.ActualReturnAt,
        Status = b.Status.ToString(),
        QuotedAmount = b.QuotedAmount,
        DepositRequired = b.DepositRequired,
        DepositPaid = b.DepositPaid,
        DiscountAmount = b.DiscountAmount,
        DeliveryFee = b.DeliveryFee,
        FinalAmount = b.FinalAmount,
        CurrencyCode = b.CurrencyCode,
        CustomerNote = b.CustomerNote,
        InternalNote = b.InternalNote,
        ExpiredAt = b.ExpiredAt,
        ConfirmedAt = b.ConfirmedAt,
        CancelledAt = b.CancelledAt,
        CancellationReason = b.CancellationReason,
        CreatedAt = b.CreatedAt,
    };
}
