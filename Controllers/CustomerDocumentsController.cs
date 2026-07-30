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
public class CustomerDocumentsController : ControllerBase
{
    private static readonly HashSet<string> AllowedImageExtensions =
        new(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".gif", ".webp" };

    private static readonly CustomerDocumentType[] RequiredForBooking =
    [
        CustomerDocumentType.IDENTITY_CARD,
        CustomerDocumentType.DRIVER_LICENSE,
    ];

    private const int MaxImageBytes = 5 * 1024 * 1024;

    private readonly ApplicationDbContext _db;
    private readonly IWebHostEnvironment _env;

    public CustomerDocumentsController(ApplicationDbContext db, IWebHostEnvironment env)
    {
        _db = db;
        _env = env;
    }

    [HttpGet("me")]
    [Authorize(Roles = AppRoles.Customer)]
    public async Task<ActionResult<IEnumerable<CustomerDocumentDto>>> GetMine()
    {
        var profile = await GetCurrentCustomerAsync();
        if (profile == null)
            return NotFound(new { message = "Customer profile not found" });

        var docs = await _db.CustomerDocuments
            .Where(x => x.CustomerId == profile.Id)
            .OrderBy(x => x.DocumentType)
            .ToListAsync();

        return Ok(docs.Select(d => ToDto(d, profile)));
    }

    [HttpPut("me/{documentType}")]
    [Authorize(Roles = AppRoles.Customer)]
    public async Task<ActionResult<CustomerDocumentDto>> UpsertMine(
        string documentType,
        [FromBody] UpsertCustomerDocumentRequest request)
    {
        var profile = await GetCurrentCustomerAsync();
        if (profile == null)
            return NotFound(new { message = "Customer profile not found" });

        if (!Enum.TryParse<CustomerDocumentType>(documentType, true, out var type))
            return BadRequest(new { message = "Invalid document type" });

        if (type == CustomerDocumentType.DRIVER_LICENSE
            && request.ExpiredAt.HasValue
            && request.ExpiredAt.Value < DateOnly.FromDateTime(DateTime.UtcNow))
        {
            return BadRequest(new { message = "Driver license is expired" });
        }

        var doc = await _db.CustomerDocuments
            .FirstOrDefaultAsync(x => x.CustomerId == profile.Id && x.DocumentType == type);

        if (doc == null)
        {
            doc = new CustomerDocument
            {
                Id = Guid.NewGuid(),
                CustomerId = profile.Id,
                DocumentType = type,
                CreatedAt = DateTime.UtcNow,
                VerificationStatus = CustomerVerificationStatus.NOT_SUBMITTED,
            };
            _db.CustomerDocuments.Add(doc);
        }

        doc.DocumentNumber = string.IsNullOrWhiteSpace(request.DocumentNumber)
            ? null
            : request.DocumentNumber.Trim();
        doc.IssuedDate = request.IssuedDate;
        doc.ExpiredAt = request.ExpiredAt;
        doc.UpdatedAt = DateTime.UtcNow;

        if (doc.VerificationStatus == CustomerVerificationStatus.VERIFIED
            || doc.VerificationStatus == CustomerVerificationStatus.REJECTED)
        {
            doc.VerificationStatus = CustomerVerificationStatus.NOT_SUBMITTED;
            doc.RejectionReason = null;
        }

        await _db.SaveChangesAsync();
        return Ok(ToDto(doc, profile));
    }

    /// <summary>side = front | back</summary>
    [HttpPost("me/{documentType}/{side}")]
    [Authorize(Roles = AppRoles.Customer)]
    [RequestSizeLimit(5 * 1024 * 1024)]
    public async Task<ActionResult<CustomerDocumentDto>> UploadImage(
        string documentType,
        string side,
        IFormFile file)
    {
        var profile = await GetCurrentCustomerAsync();
        if (profile == null)
            return NotFound(new { message = "Customer profile not found" });

        if (!Enum.TryParse<CustomerDocumentType>(documentType, true, out var type))
            return BadRequest(new { message = "Invalid document type" });

        var sideNorm = side.Trim().ToLowerInvariant();
        if (sideNorm is not ("front" or "back"))
            return BadRequest(new { message = "Side must be front or back" });

        if (file == null || file.Length == 0)
            return BadRequest(new { message = "No file uploaded" });

        var extension = Path.GetExtension(file.FileName);
        if (string.IsNullOrWhiteSpace(extension) || !AllowedImageExtensions.Contains(extension))
            return BadRequest(new { message = "Only image files are allowed (.jpg, .jpeg, .png, .gif, .webp)" });

        if (file.Length > MaxImageBytes)
            return BadRequest(new { message = "File size must be 5MB or less" });

        var doc = await _db.CustomerDocuments
            .FirstOrDefaultAsync(x => x.CustomerId == profile.Id && x.DocumentType == type);
        if (doc == null)
        {
            doc = new CustomerDocument
            {
                Id = Guid.NewGuid(),
                CustomerId = profile.Id,
                DocumentType = type,
                CreatedAt = DateTime.UtcNow,
                VerificationStatus = CustomerVerificationStatus.NOT_SUBMITTED,
            };
            _db.CustomerDocuments.Add(doc);
        }

        var folder = Path.Combine(
            _env.ContentRootPath,
            "wwwroot",
            "uploads",
            "customers",
            profile.Id.ToString("N"),
            type.ToString().ToLowerInvariant());
        Directory.CreateDirectory(folder);

        var storedFileName = $"{sideNorm}_{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
        var physicalPath = Path.Combine(folder, storedFileName);
        await using (var stream = new FileStream(physicalPath, FileMode.Create))
            await file.CopyToAsync(stream);

        var url = $"/uploads/customers/{profile.Id:N}/{type.ToString().ToLowerInvariant()}/{storedFileName}";
        if (sideNorm == "front")
        {
            TryDeleteUpload(doc.FrontImageUrl);
            doc.FrontImageUrl = url;
        }
        else
        {
            TryDeleteUpload(doc.BackImageUrl);
            doc.BackImageUrl = url;
        }

        if (doc.VerificationStatus is CustomerVerificationStatus.VERIFIED
            or CustomerVerificationStatus.REJECTED)
        {
            doc.VerificationStatus = CustomerVerificationStatus.NOT_SUBMITTED;
            doc.RejectionReason = null;
        }

        doc.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return Ok(ToDto(doc, profile));
    }

    /// <summary>
    /// Submit IDENTITY_CARD + DRIVER_LICENSE for staff review.
    /// Sets document + profile status to PENDING.
    /// </summary>
    [HttpPost("me/submit")]
    [Authorize(Roles = AppRoles.Customer)]
    public async Task<ActionResult<CustomerProfileDto>> SubmitForReview(
        [FromBody] SubmitCustomerDocumentsRequest? request)
    {
        var profile = await _db.CustomerProfiles
            .Include(x => x.Documents)
            .FirstOrDefaultAsync(x => x.UserId == GetCurrentUserId());
        if (profile == null)
            return NotFound(new { message = "Customer profile not found" });

        if (profile.IsBlacklisted)
            return BadRequest(new { message = "Account is blacklisted" });

        if (request != null)
        {
            if (!string.IsNullOrWhiteSpace(request.Gender)
                && !Enum.TryParse<CustomerGender>(request.Gender, true, out _))
                return BadRequest(new { message = "Invalid gender" });

            if (request.DateOfBirth.HasValue)
                profile.DateOfBirth = request.DateOfBirth;
            if (!string.IsNullOrWhiteSpace(request.Gender))
                profile.Gender = Enum.Parse<CustomerGender>(request.Gender, true);
            if (!string.IsNullOrWhiteSpace(request.AddressLine))
                profile.AddressLine = request.AddressLine.Trim();
            if (!string.IsNullOrWhiteSpace(request.Ward))
                profile.Ward = request.Ward.Trim();
            if (!string.IsNullOrWhiteSpace(request.Province))
                profile.Province = request.Province.Trim();
        }

        foreach (var required in RequiredForBooking)
        {
            var doc = profile.Documents.FirstOrDefault(d => d.DocumentType == required);
            if (doc == null || string.IsNullOrWhiteSpace(doc.FrontImageUrl) || string.IsNullOrWhiteSpace(doc.BackImageUrl))
            {
                return BadRequest(new
                {
                    message = $"Please upload front and back images for {required}",
                    documentType = required.ToString()
                });
            }

            if (string.IsNullOrWhiteSpace(doc.DocumentNumber))
            {
                return BadRequest(new
                {
                    message = $"Document number is required for {required}",
                    documentType = required.ToString()
                });
            }

            if (required == CustomerDocumentType.DRIVER_LICENSE
                && (!doc.ExpiredAt.HasValue || doc.ExpiredAt.Value < DateOnly.FromDateTime(DateTime.UtcNow)))
            {
                return BadRequest(new { message = "Driver license expiry is missing or expired" });
            }

            doc.VerificationStatus = CustomerVerificationStatus.PENDING;
            doc.RejectionReason = null;
            doc.UpdatedAt = DateTime.UtcNow;
        }

        profile.VerificationStatus = CustomerVerificationStatus.PENDING;
        profile.VerifiedAt = null;
        profile.VerifiedBy = null;
        profile.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return Ok(CustomerProfilesController.ToDto(profile));
    }

    [HttpGet("pending")]
    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Staff}")]
    public async Task<ActionResult<IEnumerable<CustomerDocumentDto>>> GetPending()
    {
        var list = await _db.CustomerDocuments
            .Include(x => x.Customer)
            .Where(x => x.VerificationStatus == CustomerVerificationStatus.PENDING)
            .OrderBy(x => x.UpdatedAt ?? x.CreatedAt)
            .ToListAsync();

        return Ok(list.Select(d => ToDto(d, d.Customer)));
    }

    [HttpGet]
    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Staff}")]
    public async Task<ActionResult<IEnumerable<CustomerDocumentDto>>> GetAll(
        [FromQuery] string? status = null,
        [FromQuery] Guid? customerId = null)
    {
        var query = _db.CustomerDocuments.Include(x => x.Customer).AsQueryable();

        if (!string.IsNullOrWhiteSpace(status)
            && Enum.TryParse<CustomerVerificationStatus>(status, true, out var parsed))
            query = query.Where(x => x.VerificationStatus == parsed);

        if (customerId.HasValue)
            query = query.Where(x => x.CustomerId == customerId.Value);

        var list = await query.OrderByDescending(x => x.UpdatedAt ?? x.CreatedAt).ToListAsync();
        return Ok(list.Select(d => ToDto(d, d.Customer)));
    }

    [HttpPut("{id:guid}/review")]
    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Staff}")]
    public async Task<ActionResult<CustomerDocumentDto>> Review(
        Guid id,
        [FromBody] ReviewCustomerDocumentRequest request)
    {
        var doc = await _db.CustomerDocuments
            .Include(x => x.Customer)!
            .ThenInclude(c => c!.Documents)
            .FirstOrDefaultAsync(x => x.Id == id);
        if (doc == null)
            return NotFound(new { message = "Document not found" });

        if (doc.VerificationStatus != CustomerVerificationStatus.PENDING)
            return BadRequest(new { message = "Only PENDING documents can be reviewed" });

        if (!Enum.TryParse<CustomerVerificationStatus>(request.Status, true, out var status)
            || status is not (CustomerVerificationStatus.VERIFIED or CustomerVerificationStatus.REJECTED))
        {
            return BadRequest(new { message = "Status must be VERIFIED or REJECTED" });
        }

        doc.VerificationStatus = status;
        doc.RejectionReason = status == CustomerVerificationStatus.REJECTED
            ? (string.IsNullOrWhiteSpace(request.RejectionReason)
                ? "Rejected"
                : request.RejectionReason.Trim())
            : null;
        doc.UpdatedAt = DateTime.UtcNow;

        var customer = doc.Customer!;
        RecalculateCustomerVerification(customer);
        customer.UpdatedAt = DateTime.UtcNow;
        if (customer.VerificationStatus == CustomerVerificationStatus.VERIFIED)
        {
            customer.VerifiedAt = DateTime.UtcNow;
            customer.VerifiedBy = GetCurrentUserId();
        }
        else if (status == CustomerVerificationStatus.REJECTED)
        {
            customer.VerifiedAt = null;
            customer.VerifiedBy = null;
        }

        await _db.SaveChangesAsync();
        return Ok(ToDto(doc, customer));
    }

    private static void RecalculateCustomerVerification(CustomerProfile customer)
    {
        var required = customer.Documents
            .Where(d => RequiredForBooking.Contains(d.DocumentType))
            .ToList();

        if (required.Count < RequiredForBooking.Length)
        {
            customer.VerificationStatus = CustomerVerificationStatus.NOT_SUBMITTED;
            return;
        }

        if (required.Any(d => d.VerificationStatus == CustomerVerificationStatus.REJECTED))
        {
            customer.VerificationStatus = CustomerVerificationStatus.REJECTED;
            return;
        }

        if (required.Any(d => d.VerificationStatus == CustomerVerificationStatus.PENDING))
        {
            customer.VerificationStatus = CustomerVerificationStatus.PENDING;
            return;
        }

        if (required.All(d => d.VerificationStatus == CustomerVerificationStatus.VERIFIED))
        {
            if (required.Any(d =>
                    d.DocumentType == CustomerDocumentType.DRIVER_LICENSE
                    && d.ExpiredAt.HasValue
                    && d.ExpiredAt.Value < DateOnly.FromDateTime(DateTime.UtcNow)))
            {
                customer.VerificationStatus = CustomerVerificationStatus.EXPIRED;
                return;
            }

            customer.VerificationStatus = CustomerVerificationStatus.VERIFIED;
            return;
        }

        customer.VerificationStatus = CustomerVerificationStatus.NOT_SUBMITTED;
    }

    private async Task<CustomerProfile?> GetCurrentCustomerAsync()
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return null;
        return await _db.CustomerProfiles.FirstOrDefaultAsync(x => x.UserId == userId);
    }

    private string? GetCurrentUserId() =>
        User.FindFirstValue(ClaimTypes.NameIdentifier);

    private void TryDeleteUpload(string? url)
    {
        if (string.IsNullOrWhiteSpace(url) ||
            !url.StartsWith("/uploads/customers/", StringComparison.OrdinalIgnoreCase))
            return;

        var path = Path.Combine(
            _env.ContentRootPath,
            "wwwroot",
            url.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
        if (System.IO.File.Exists(path))
        {
            try { System.IO.File.Delete(path); } catch { /* ignore */ }
        }
    }

    internal static CustomerDocumentDto ToDto(CustomerDocument d, CustomerProfile? customer) => new()
    {
        Id = d.Id,
        CustomerId = d.CustomerId,
        CustomerCode = customer?.CustomerCode ?? string.Empty,
        CustomerName = customer?.FullName ?? string.Empty,
        DocumentType = d.DocumentType.ToString(),
        DocumentNumber = d.DocumentNumber,
        FrontImageUrl = d.FrontImageUrl,
        BackImageUrl = d.BackImageUrl,
        IssuedDate = d.IssuedDate,
        ExpiredAt = d.ExpiredAt,
        VerificationStatus = d.VerificationStatus.ToString(),
        RejectionReason = d.RejectionReason,
        CreatedAt = d.CreatedAt,
        UpdatedAt = d.UpdatedAt,
    };
}
