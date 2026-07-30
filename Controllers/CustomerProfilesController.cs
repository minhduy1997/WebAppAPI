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
public class CustomerProfilesController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public CustomerProfilesController(ApplicationDbContext db)
    {
        _db = db;
    }

    [HttpGet("lookups")]
    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Staff},{AppRoles.Customer}")]
    public ActionResult<BookingLookupsDto> GetLookups()
    {
        return Ok(new BookingLookupsDto
        {
            Statuses = Enum.GetNames<BookingStatus>(),
            VerificationStatuses = Enum.GetNames<CustomerVerificationStatus>(),
            Genders = Enum.GetNames<CustomerGender>(),
            DocumentTypes = Enum.GetNames<CustomerDocumentType>(),
        });
    }

    [HttpGet("me")]
    [Authorize(Roles = AppRoles.Customer)]
    public async Task<ActionResult<CustomerProfileDto>> GetMine()
    {
        var profile = await GetCurrentCustomerProfileAsync(includeDocuments: true);
        if (profile == null)
            return NotFound(new { message = "Customer profile not found" });

        return Ok(ToDto(profile));
    }

    [HttpPut("me")]
    [Authorize(Roles = AppRoles.Customer)]
    public async Task<ActionResult<CustomerProfileDto>> UpdateMine([FromBody] UpdateCustomerBasicRequest request)
    {
        var profile = await GetCurrentCustomerProfileAsync(includeDocuments: true, includeUser: true);
        if (profile == null)
            return NotFound(new { message = "Customer profile not found" });

        var phone = NormalizePhone(request.PhoneNumber);
        if (string.IsNullOrWhiteSpace(phone))
            return BadRequest(new { message = "Phone number is required" });

        if (await _db.CustomerProfiles.AnyAsync(x => x.PhoneNumber == phone && x.Id != profile.Id))
            return BadRequest(new { message = "Phone number already registered" });

        if (!string.IsNullOrWhiteSpace(request.Gender)
            && !Enum.TryParse<CustomerGender>(request.Gender, true, out _))
            return BadRequest(new { message = "Invalid gender. Use MALE, FEMALE, or OTHER" });

        profile.FullName = request.FullName.Trim();
        profile.PhoneNumber = phone;
        profile.DateOfBirth = request.DateOfBirth;
        profile.Gender = string.IsNullOrWhiteSpace(request.Gender)
            ? profile.Gender
            : Enum.Parse<CustomerGender>(request.Gender, true);
        profile.AddressLine = string.IsNullOrWhiteSpace(request.AddressLine) ? null : request.AddressLine.Trim();
        profile.Ward = string.IsNullOrWhiteSpace(request.Ward) ? null : request.Ward.Trim();
        profile.Province = string.IsNullOrWhiteSpace(request.Province) ? null : request.Province.Trim();
        profile.UpdatedAt = DateTime.UtcNow;

        if (profile.User != null)
        {
            profile.User.FullName = profile.FullName;
            profile.User.PhoneNumber = phone;
        }

        await _db.SaveChangesAsync();
        return Ok(ToDto(profile));
    }

    [HttpGet]
    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Staff}")]
    public async Task<ActionResult<IEnumerable<CustomerProfileDto>>> GetAll(
        [FromQuery] string? verificationStatus = null,
        [FromQuery] string? search = null)
    {
        var query = _db.CustomerProfiles.Include(x => x.Documents).AsQueryable();

        if (!string.IsNullOrWhiteSpace(verificationStatus)
            && Enum.TryParse<CustomerVerificationStatus>(verificationStatus, true, out var status))
        {
            query = query.Where(x => x.VerificationStatus == status);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var q = search.Trim();
            query = query.Where(x =>
                x.FullName.Contains(q) ||
                x.Email.Contains(q) ||
                x.PhoneNumber.Contains(q) ||
                x.CustomerCode.Contains(q));
        }

        var list = await query.OrderByDescending(x => x.CreatedAt).ToListAsync();
        return Ok(list.Select(ToDto));
    }

    [HttpGet("{id:guid}")]
    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Staff}")]
    public async Task<ActionResult<CustomerProfileDto>> GetById(Guid id)
    {
        var profile = await _db.CustomerProfiles
            .Include(x => x.Documents)
            .FirstOrDefaultAsync(x => x.Id == id);
        if (profile == null)
            return NotFound(new { message = "Customer profile not found" });

        return Ok(ToDto(profile));
    }

    [HttpPut("{id:guid}/blacklist")]
    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Staff}")]
    public async Task<ActionResult<CustomerProfileDto>> SetBlacklist(
        Guid id,
        [FromBody] SetCustomerBlacklistRequest request)
    {
        var profile = await _db.CustomerProfiles
            .Include(x => x.Documents)
            .FirstOrDefaultAsync(x => x.Id == id);
        if (profile == null)
            return NotFound(new { message = "Customer profile not found" });

        profile.IsBlacklisted = request.IsBlacklisted;
        profile.BlacklistReason = request.IsBlacklisted
            ? (string.IsNullOrWhiteSpace(request.BlacklistReason)
                ? profile.BlacklistReason
                : request.BlacklistReason.Trim())
            : null;
        profile.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return Ok(ToDto(profile));
    }

    private async Task<CustomerProfile?> GetCurrentCustomerProfileAsync(
        bool includeDocuments = false,
        bool includeUser = false)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
            return null;

        var query = _db.CustomerProfiles.AsQueryable();
        if (includeDocuments)
            query = query.Include(x => x.Documents);
        if (includeUser)
            query = query.Include(x => x.User);

        return await query.FirstOrDefaultAsync(x => x.UserId == userId);
    }

    private static string NormalizePhone(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;
        return new string(value.Where(c => char.IsDigit(c) || c == '+').ToArray());
    }

    internal static CustomerProfileDto ToDto(CustomerProfile p) => new()
    {
        Id = p.Id,
        UserId = p.UserId,
        CustomerCode = p.CustomerCode,
        FullName = p.FullName,
        DateOfBirth = p.DateOfBirth,
        Gender = p.Gender?.ToString(),
        PhoneNumber = p.PhoneNumber,
        Email = p.Email,
        AddressLine = p.AddressLine,
        Ward = p.Ward,
        Province = p.Province,
        VerificationStatus = p.VerificationStatus.ToString(),
        VerifiedAt = p.VerifiedAt,
        IsBlacklisted = p.IsBlacklisted,
        BlacklistReason = p.BlacklistReason,
        Note = p.Note,
        Documents = (p.Documents ?? [])
            .OrderBy(d => d.DocumentType)
            .Select(d => CustomerDocumentsController.ToDto(d, p))
            .ToList(),
    };
}
