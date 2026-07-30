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
public class VehicleModelsController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public VehicleModelsController(ApplicationDbContext db)
    {
        _db = db;
    }

    [HttpGet("lookups")]
    public async Task<ActionResult<VehicleModelLookupsDto>> GetLookups()
    {
        var categories = await _db.VehicleCategories
            .Where(x => x.IsActive)
            .OrderBy(x => x.Name)
            .Select(x => new VehicleCategoriesDto
            {
                Id = x.Id,
                Name = x.Name,
                Description = x.Description,
                IsActive = x.IsActive,
            })
            .ToListAsync();

        return Ok(new VehicleModelLookupsDto
        {
            Categories = categories,
            TransmissionTypes = Enum.GetNames<TransmissionType>(),
            FuelTypes = Enum.GetNames<FuelType>(),
            Drivetrains = new[] { "FWD", "RWD", "AWD", "4WD" },
        });
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<VehicleModelDto>>> GetAll()
    {
        var list = await _db.VehicleModels
            .Include(x => x.Category)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Brand)
            .ThenBy(x => x.ModelName)
            .ToListAsync();

        return Ok(list.Select(ToDto));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<VehicleModelDto>> GetById(Guid id)
    {
        var item = await _db.VehicleModels
            .Include(x => x.Category)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (item == null)
            return NotFound(new { message = "Vehicle model not found" });

        return Ok(ToDto(item));
    }

    [HttpPost]
    public async Task<ActionResult<VehicleModelDto>> Create([FromBody] VehicleModelRequest request)
    {
        var validation = await ValidateRequest(request);
        if (validation != null)
            return validation;

        var code = request.Code.Trim().ToUpperInvariant();
        if (await _db.VehicleModels.AnyAsync(x => x.Code == code))
            return BadRequest(new { message = "Vehicle model code already exists" });

        var item = MapToEntity(new VehicleModel { Id = Guid.NewGuid() }, request);
        item.Code = code;
        item.CreatedAt = DateTime.UtcNow;
        item.CreatedBy = GetCurrentUserId();

        _db.VehicleModels.Add(item);
        await _db.SaveChangesAsync();

        await _db.Entry(item).Reference(x => x.Category).LoadAsync();
        return Ok(ToDto(item));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<VehicleModelDto>> Update(Guid id, [FromBody] VehicleModelRequest request)
    {
        var item = await _db.VehicleModels.Include(x => x.Category).FirstOrDefaultAsync(x => x.Id == id);
        if (item == null)
            return NotFound(new { message = "Vehicle model not found" });

        var validation = await ValidateRequest(request);
        if (validation != null)
            return validation;

        var code = request.Code.Trim().ToUpperInvariant();
        if (!string.Equals(item.Code, code, StringComparison.OrdinalIgnoreCase)
            && await _db.VehicleModels.AnyAsync(x => x.Code == code))
        {
            return BadRequest(new { message = "Vehicle model code already exists" });
        }

        MapToEntity(item, request);
        item.Code = code;
        item.UpdatedAt = DateTime.UtcNow;
        item.UpdatedBy = GetCurrentUserId();

        await _db.SaveChangesAsync();
        await _db.Entry(item).Reference(x => x.Category).LoadAsync();
        return Ok(ToDto(item));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var item = await _db.VehicleModels.FindAsync(id);
        if (item == null)
            return NotFound(new { message = "Vehicle model not found" });

        _db.VehicleModels.Remove(item);
        await _db.SaveChangesAsync();
        return Ok(new { message = "Vehicle model deleted" });
    }

    private async Task<ActionResult?> ValidateRequest(VehicleModelRequest request)
    {
        if (!await _db.VehicleCategories.AnyAsync(x => x.Id == request.CategoryId && x.IsActive))
            return BadRequest(new { message = "Invalid or inactive vehicle category" });

        if (!Enum.TryParse<TransmissionType>(request.TransmissionType, true, out _))
            return BadRequest(new { message = "Invalid transmission type. Use MT or AT" });

        if (!Enum.TryParse<FuelType>(request.FuelType, true, out _))
            return BadRequest(new { message = "Invalid fuel type. Use RONE10, DIESEL, or ELECTRIC" });

        if (!string.IsNullOrWhiteSpace(request.Drivetrain))
        {
            var d = request.Drivetrain.Trim().ToUpperInvariant();
            if (d is not ("FWD" or "RWD" or "AWD" or "4WD"))
                return BadRequest(new { message = "Invalid drivetrain. Use FWD, RWD, AWD, or 4WD" });
        }

        return null;
    }

    private static VehicleModel MapToEntity(VehicleModel item, VehicleModelRequest request)
    {
        item.Brand = request.Brand.Trim();
        item.ModelName = request.ModelName.Trim();
        item.VariantName = string.IsNullOrWhiteSpace(request.VariantName) ? null : request.VariantName.Trim();
        item.ManufactureYear = request.ManufactureYear;
        item.CategoryId = request.CategoryId;
        item.SeatCount = request.SeatCount;
        item.DoorCount = request.DoorCount;
        item.TransmissionType = Enum.Parse<TransmissionType>(request.TransmissionType, true);
        item.FuelType = Enum.Parse<FuelType>(request.FuelType, true);
        item.EngineCapacity = request.EngineCapacity;
        item.Drivetrain = ParseDrivetrain(request.Drivetrain);
        item.LuggageCapacity = request.LuggageCapacity;
        item.FuelConsumption = request.FuelConsumption;
        item.BaseDailyPrice = request.BaseDailyPrice;
        item.WeekendDailyPrice = request.WeekendDailyPrice;
        item.HolidayDailyPrice = request.HolidayDailyPrice;
        item.DepositAmount = request.DepositAmount;
        item.IncludedKmPerDay = request.IncludedKmPerDay;
        item.ExtraKmPrice = request.ExtraKmPrice;
        item.LateHourPrice = request.LateHourPrice;
        item.ThumbnailUrl = string.IsNullOrWhiteSpace(request.ThumbnailUrl) ? null : request.ThumbnailUrl.Trim();
        item.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        item.IsPublished = request.IsPublished;
        item.IsActive = request.IsActive;
        item.SortOrder = request.SortOrder;
        return item;
    }

    private static Drivetrain? ParseDrivetrain(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var v = value.Trim().ToUpperInvariant();
        return v switch
        {
            "FWD" => Drivetrain.FWD,
            "RWD" => Drivetrain.RWD,
            "AWD" => Drivetrain.AWD,
            "4WD" => Drivetrain.FourWD,
            _ => null,
        };
    }

    private static string? FormatDrivetrain(Drivetrain? value) =>
        value switch
        {
            null => null,
            Drivetrain.FourWD => "4WD",
            _ => value.ToString(),
        };

    private Guid? GetCurrentUserId()
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(raw, out var id) ? id : null;
    }

    private static VehicleModelDto ToDto(VehicleModel entity) => new()
    {
        Id = entity.Id,
        Code = entity.Code,
        Brand = entity.Brand,
        ModelName = entity.ModelName,
        VariantName = entity.VariantName,
        ManufactureYear = entity.ManufactureYear,
        CategoryId = entity.CategoryId,
        CategoryName = entity.Category?.Name ?? string.Empty,
        SeatCount = entity.SeatCount,
        DoorCount = entity.DoorCount,
        TransmissionType = entity.TransmissionType.ToString(),
        FuelType = entity.FuelType.ToString(),
        EngineCapacity = entity.EngineCapacity,
        Drivetrain = FormatDrivetrain(entity.Drivetrain),
        LuggageCapacity = entity.LuggageCapacity,
        FuelConsumption = entity.FuelConsumption,
        BaseDailyPrice = entity.BaseDailyPrice,
        WeekendDailyPrice = entity.WeekendDailyPrice,
        HolidayDailyPrice = entity.HolidayDailyPrice,
        DepositAmount = entity.DepositAmount,
        IncludedKmPerDay = entity.IncludedKmPerDay,
        ExtraKmPrice = entity.ExtraKmPrice,
        LateHourPrice = entity.LateHourPrice,
        ThumbnailUrl = entity.ThumbnailUrl,
        Description = entity.Description,
        IsPublished = entity.IsPublished,
        IsActive = entity.IsActive,
        SortOrder = entity.SortOrder,
    };
}
