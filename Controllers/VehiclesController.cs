using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebAppApi.Data;
using WebAppApi.Models;

namespace WebAppApi.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/[controller]")]
public class VehiclesController : ControllerBase
{
    private static readonly HashSet<string> AllowedImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".gif", ".webp"
    };

    private const int MaxImageBytes = 5 * 1024 * 1024;
    private const int MaxImagesPerUpload = 20;

    private readonly ApplicationDbContext _db;
    private readonly IWebHostEnvironment _env;

    public VehiclesController(ApplicationDbContext db, IWebHostEnvironment env)
    {
        _db = db;
        _env = env;
    }

    [HttpGet("lookups")]
    public async Task<ActionResult<VehicleLookupsDto>> GetLookups()
    {
        var models = await _db.VehicleModels
            .Where(x => x.IsActive)
            .OrderBy(x => x.Brand)
            .ThenBy(x => x.ModelName)
            .Select(x => new VehicleModelLookupItemDto
            {
                Id = x.Id,
                Code = x.Code,
                Brand = x.Brand,
                ModelName = x.ModelName,
                DisplayName = x.VariantName == null
                    ? $"{x.Brand} {x.ModelName} ({x.Code})"
                    : $"{x.Brand} {x.ModelName} {x.VariantName} ({x.Code})",
            })
            .ToListAsync();

        return Ok(new VehicleLookupsDto
        {
            Models = models,
            Statuses = Enum.GetNames<VehicleStatus>(),
        });
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<VehicleDto>>> GetAll()
    {
        var list = await _db.Vehicles
            .Include(x => x.VehicleModel)
            .Include(x => x.Images)
            .OrderBy(x => x.Code)
            .ToListAsync();

        return Ok(list.Select(ToDto));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<VehicleDto>> GetById(Guid id)
    {
        var item = await _db.Vehicles
            .Include(x => x.VehicleModel)
            .Include(x => x.Images)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (item == null)
            return NotFound(new { message = "Vehicle not found" });

        return Ok(ToDto(item));
    }

    [HttpPost]
    public async Task<ActionResult<VehicleDto>> Create([FromBody] VehicleRequest request)
    {
        var validation = await ValidateRequest(request, excludeId: null);
        if (validation != null)
            return validation;

        var model = await _db.VehicleModels.FirstAsync(x => x.Id == request.VehicleModelId);
        var code = await GenerateNextCodeAsync(model.Code);

        var item = MapToEntity(new Vehicle { Id = Guid.NewGuid() }, request);
        item.Code = code;
        item.CreatedAt = DateTime.UtcNow;
        item.CreatedBy = GetCurrentUserId();

        _db.Vehicles.Add(item);
        await _db.SaveChangesAsync();

        await _db.Entry(item).Reference(x => x.VehicleModel).LoadAsync();
        await _db.Entry(item).Collection(x => x.Images).LoadAsync();
        return Ok(ToDto(item));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<VehicleDto>> Update(Guid id, [FromBody] VehicleRequest request)
    {
        var item = await _db.Vehicles
            .Include(x => x.VehicleModel)
            .Include(x => x.Images)
            .FirstOrDefaultAsync(x => x.Id == id);
        if (item == null)
            return NotFound(new { message = "Vehicle not found" });

        var validation = await ValidateRequest(request, excludeId: id);
        if (validation != null)
            return validation;

        if (item.VehicleModelId != request.VehicleModelId)
        {
            var model = await _db.VehicleModels.FirstAsync(x => x.Id == request.VehicleModelId);
            item.Code = await GenerateNextCodeAsync(model.Code);
        }

        MapToEntity(item, request);
        item.UpdatedAt = DateTime.UtcNow;
        item.UpdatedBy = GetCurrentUserId();

        await _db.SaveChangesAsync();
        await _db.Entry(item).Reference(x => x.VehicleModel).LoadAsync();
        return Ok(ToDto(item));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var item = await _db.Vehicles
            .Include(x => x.Images)
            .FirstOrDefaultAsync(x => x.Id == id);
        if (item == null)
            return NotFound(new { message = "Vehicle not found" });

        foreach (var image in item.Images)
            TryDeletePhysicalFile(image.Url);

        DeleteVehicleUploadFolder(id);

        _db.Vehicles.Remove(item);
        await _db.SaveChangesAsync();
        return Ok(new { message = "Vehicle deleted" });
    }

    [HttpPost("{id:guid}/images")]
    [RequestSizeLimit(30 * 1024 * 1024)]
    public async Task<ActionResult<IEnumerable<VehicleImageDto>>> UploadImages(Guid id, List<IFormFile> files)
    {
        var vehicle = await _db.Vehicles
            .Include(x => x.Images)
            .FirstOrDefaultAsync(x => x.Id == id);
        if (vehicle == null)
            return NotFound(new { message = "Vehicle not found" });

        if (files == null || files.Count == 0)
            return BadRequest(new { message = "No files uploaded" });

        if (files.Count > MaxImagesPerUpload)
            return BadRequest(new { message = $"Maximum {MaxImagesPerUpload} images per upload" });

        var uploadsFolder = Path.Combine(_env.ContentRootPath, "wwwroot", "uploads", "vehicles", id.ToString("N"));
        Directory.CreateDirectory(uploadsFolder);

        var nextSort = vehicle.Images.Count == 0 ? 0 : vehicle.Images.Max(x => x.SortOrder) + 1;
        var created = new List<VehicleImage>();
        var userId = GetCurrentUserId();
        var makePrimary = !vehicle.Images.Any(x => x.IsPrimary);

        foreach (var file in files)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { message = "One or more files are empty" });

            var extension = Path.GetExtension(file.FileName);
            if (string.IsNullOrWhiteSpace(extension) || !AllowedImageExtensions.Contains(extension))
                return BadRequest(new { message = "Only image files are allowed (.jpg, .jpeg, .png, .gif, .webp)" });

            if (file.Length > MaxImageBytes)
                return BadRequest(new { message = "Each image must be 5MB or less" });

            var originalName = Path.GetFileNameWithoutExtension(file.FileName);
            foreach (var c in Path.GetInvalidFileNameChars())
                originalName = originalName.Replace(c, '_');
            if (string.IsNullOrWhiteSpace(originalName))
                originalName = "vehicle";

            var storedFileName = $"{originalName}_{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
            var physicalPath = Path.Combine(uploadsFolder, storedFileName);

            await using (var stream = new FileStream(physicalPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var image = new VehicleImage
            {
                Id = Guid.NewGuid(),
                VehicleId = id,
                Url = $"/uploads/vehicles/{id:N}/{storedFileName}",
                OriginalFileName = Path.GetFileName(file.FileName),
                SortOrder = nextSort++,
                IsPrimary = makePrimary,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = userId,
            };
            makePrimary = false;
            created.Add(image);
            _db.VehicleImages.Add(image);
        }

        await _db.SaveChangesAsync();
        return Ok(created.Select(ToImageDto));
    }

    [HttpDelete("{vehicleId:guid}/images/{imageId:guid}")]
    public async Task<IActionResult> DeleteImage(Guid vehicleId, Guid imageId)
    {
        var image = await _db.VehicleImages
            .FirstOrDefaultAsync(x => x.Id == imageId && x.VehicleId == vehicleId);
        if (image == null)
            return NotFound(new { message = "Image not found" });

        TryDeletePhysicalFile(image.Url);

        var wasPrimary = image.IsPrimary;
        _db.VehicleImages.Remove(image);
        await _db.SaveChangesAsync();

        if (wasPrimary)
        {
            var next = await _db.VehicleImages
                .Where(x => x.VehicleId == vehicleId)
                .OrderBy(x => x.SortOrder)
                .FirstOrDefaultAsync();
            if (next != null)
            {
                next.IsPrimary = true;
                await _db.SaveChangesAsync();
            }
        }

        return Ok(new { message = "Image deleted" });
    }

    private async Task<ActionResult?> ValidateRequest(VehicleRequest request, Guid? excludeId)
    {
        if (!await _db.VehicleModels.AnyAsync(x => x.Id == request.VehicleModelId && x.IsActive))
            return BadRequest(new { message = "Invalid or inactive vehicle model" });

        if (!Enum.TryParse<VehicleStatus>(request.Status, true, out _))
            return BadRequest(new
            {
                message = "Invalid status. Use AVAILABLE, RESERVED, RENTED, MAINTENANCE, REPAIRING, UNAVAILABLE, or INACTIVE"
            });

        if (request.FuelLevel is < 0 or > 100)
            return BadRequest(new { message = "Fuel level must be between 0 and 100" });

        if (request.CurrentOdometer < 0)
            return BadRequest(new { message = "Current odometer must be >= 0" });

        var plate = NormalizeLicensePlate(request.LicensePlate);
        var plateExists = await _db.Vehicles.AnyAsync(x =>
            x.LicensePlate == plate && (excludeId == null || x.Id != excludeId));
        if (plateExists)
            return BadRequest(new { message = "License plate already exists" });

        return null;
    }

    private async Task<string> GenerateNextCodeAsync(string modelCode)
    {
        var prefix = modelCode.Trim().ToUpperInvariant();
        var existing = await _db.Vehicles
            .Where(x => x.Code.StartsWith(prefix + "-"))
            .Select(x => x.Code)
            .ToListAsync();

        var next = 1;
        foreach (var code in existing)
        {
            var suffix = code[(prefix.Length + 1)..];
            if (int.TryParse(suffix, out var n) && n >= next)
                next = n + 1;
        }

        return $"{prefix}-{next:D3}";
    }

    private static Vehicle MapToEntity(Vehicle item, VehicleRequest request)
    {
        item.VehicleModelId = request.VehicleModelId;
        item.LicensePlate = NormalizeLicensePlate(request.LicensePlate);
        item.VinNumber = string.IsNullOrWhiteSpace(request.VinNumber) ? null : request.VinNumber.Trim();
        item.EngineNumber = string.IsNullOrWhiteSpace(request.EngineNumber) ? null : request.EngineNumber.Trim();
        item.Color = string.IsNullOrWhiteSpace(request.Color) ? null : request.Color.Trim();
        item.ManufactureYear = request.ManufactureYear;
        item.RegistrationDate = request.RegistrationDate;
        item.CurrentOdometer = request.CurrentOdometer;
        item.FuelLevel = request.FuelLevel;
        item.LocationId = request.LocationId;
        item.Status = Enum.Parse<VehicleStatus>(request.Status, true);
        item.RegistrationExpiredAt = request.RegistrationExpiredAt;
        item.InsuranceExpiredAt = request.InsuranceExpiredAt;
        item.MaintenanceDueAt = request.MaintenanceDueAt;
        item.MaintenanceDueOdometer = request.MaintenanceDueOdometer;
        item.PurchaseDate = request.PurchaseDate;
        item.PurchasePrice = request.PurchasePrice;
        item.RentalDailyPriceOverride = request.RentalDailyPriceOverride;
        item.Note = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim();
        item.IsActive = request.IsActive;
        return item;
    }

    private static string NormalizeLicensePlate(string value) => value.Trim().ToUpperInvariant();

    private Guid? GetCurrentUserId()
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(raw, out var id) ? id : null;
    }

    private void TryDeletePhysicalFile(string url)
    {
        if (string.IsNullOrWhiteSpace(url) ||
            !url.StartsWith("/uploads/vehicles/", StringComparison.OrdinalIgnoreCase))
            return;

        var physicalPath = Path.Combine(
            _env.ContentRootPath,
            "wwwroot",
            url.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));

        if (System.IO.File.Exists(physicalPath))
        {
            try { System.IO.File.Delete(physicalPath); } catch { /* ignore */ }
        }
    }

    private void DeleteVehicleUploadFolder(Guid vehicleId)
    {
        var folder = Path.Combine(_env.ContentRootPath, "wwwroot", "uploads", "vehicles", vehicleId.ToString("N"));
        if (Directory.Exists(folder))
        {
            try { Directory.Delete(folder, recursive: true); } catch { /* ignore */ }
        }
    }

    private static VehicleImageDto ToImageDto(VehicleImage image) => new()
    {
        Id = image.Id,
        Url = image.Url,
        OriginalFileName = image.OriginalFileName,
        SortOrder = image.SortOrder,
        IsPrimary = image.IsPrimary,
    };

    private static VehicleDto ToDto(Vehicle entity) => new()
    {
        Id = entity.Id,
        VehicleModelId = entity.VehicleModelId,
        VehicleModelCode = entity.VehicleModel?.Code ?? string.Empty,
        VehicleModelName = entity.VehicleModel == null
            ? string.Empty
            : string.IsNullOrWhiteSpace(entity.VehicleModel.VariantName)
                ? $"{entity.VehicleModel.Brand} {entity.VehicleModel.ModelName}"
                : $"{entity.VehicleModel.Brand} {entity.VehicleModel.ModelName} {entity.VehicleModel.VariantName}",
        Code = entity.Code,
        LicensePlate = entity.LicensePlate,
        VinNumber = entity.VinNumber,
        EngineNumber = entity.EngineNumber,
        Color = entity.Color,
        ManufactureYear = entity.ManufactureYear,
        RegistrationDate = entity.RegistrationDate,
        CurrentOdometer = entity.CurrentOdometer,
        FuelLevel = entity.FuelLevel,
        LocationId = entity.LocationId,
        Status = entity.Status.ToString(),
        RegistrationExpiredAt = entity.RegistrationExpiredAt,
        InsuranceExpiredAt = entity.InsuranceExpiredAt,
        MaintenanceDueAt = entity.MaintenanceDueAt,
        MaintenanceDueOdometer = entity.MaintenanceDueOdometer,
        PurchaseDate = entity.PurchaseDate,
        PurchasePrice = entity.PurchasePrice,
        RentalDailyPriceOverride = entity.RentalDailyPriceOverride,
        Note = entity.Note,
        IsActive = entity.IsActive,
        Images = entity.Images
            .OrderBy(x => x.SortOrder)
            .Select(ToImageDto)
            .ToList(),
    };
}
