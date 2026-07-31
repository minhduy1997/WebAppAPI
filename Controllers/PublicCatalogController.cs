using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebAppApi.Data;
using WebAppApi.Models;

namespace WebAppApi.Controllers;

/// <summary>
/// Anonymous read-only catalog for the customer website.
/// Only published + active models; no admin/ops fields.
/// </summary>
[ApiController]
[AllowAnonymous]
[Route("api/public")]
public class PublicCatalogController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public PublicCatalogController(ApplicationDbContext db)
    {
        _db = db;
    }

    /// <summary>Active categories that have at least one published model.</summary>
    [HttpGet("categories")]
    public async Task<ActionResult<IEnumerable<PublicCategoryDto>>> GetCategories()
    {
        var list = await _db.VehicleCategories
            .AsNoTracking()
            .Where(c => c.IsActive
                && c.VehicleModels.Any(m => m.IsActive && m.IsPublished))
            .OrderBy(c => c.Name)
            .Select(c => new PublicCategoryDto
            {
                Id = c.Id,
                Name = c.Name,
                Description = c.Description,
            })
            .ToListAsync();

        return Ok(list);
    }

    /// <summary>Filter options for browse UI.</summary>
    [HttpGet("lookups")]
    public async Task<ActionResult<PublicCatalogLookupsDto>> GetLookups()
    {
        var published = PublishedModels();

        var categories = await _db.VehicleCategories
            .AsNoTracking()
            .Where(c => c.IsActive
                && c.VehicleModels.Any(m => m.IsActive && m.IsPublished))
            .OrderBy(c => c.Name)
            .Select(c => new PublicCategoryDto
            {
                Id = c.Id,
                Name = c.Name,
                Description = c.Description,
            })
            .ToListAsync();

        var brands = await published
            .Select(m => m.Brand)
            .Distinct()
            .OrderBy(b => b)
            .ToListAsync();

        return Ok(new PublicCatalogLookupsDto
        {
            Categories = categories,
            Brands = brands,
            TransmissionTypes = Enum.GetNames<TransmissionType>(),
            FuelTypes = Enum.GetNames<FuelType>(),
            Drivetrains = new[] { "FWD", "RWD", "AWD", "4WD" },
        });
    }

    /// <summary>
    /// List published models. Optional filters:
    /// categoryId, brand, seatCount, transmissionType, fuelType, q (brand/model/variant).
    /// </summary>
    [HttpGet("vehicle-models")]
    public async Task<ActionResult<IEnumerable<PublicVehicleModelSummaryDto>>> GetVehicleModels(
        [FromQuery] int? categoryId = null,
        [FromQuery] string? brand = null,
        [FromQuery] byte? seatCount = null,
        [FromQuery] string? transmissionType = null,
        [FromQuery] string? fuelType = null,
        [FromQuery] string? q = null)
    {
        var query = PublishedModels().Include(m => m.Category).AsQueryable();

        if (categoryId.HasValue)
            query = query.Where(m => m.CategoryId == categoryId.Value);

        if (!string.IsNullOrWhiteSpace(brand))
        {
            var b = brand.Trim();
            query = query.Where(m => m.Brand == b);
        }

        if (seatCount.HasValue)
            query = query.Where(m => m.SeatCount == seatCount.Value);

        if (!string.IsNullOrWhiteSpace(transmissionType)
            && Enum.TryParse<TransmissionType>(transmissionType, true, out var transmission))
        {
            query = query.Where(m => m.TransmissionType == transmission);
        }

        if (!string.IsNullOrWhiteSpace(fuelType)
            && Enum.TryParse<FuelType>(fuelType, true, out var fuel))
        {
            query = query.Where(m => m.FuelType == fuel);
        }

        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            query = query.Where(m =>
                m.Brand.Contains(term)
                || m.ModelName.Contains(term)
                || (m.VariantName != null && m.VariantName.Contains(term)));
        }

        var list = await query
            .OrderBy(m => m.SortOrder)
            .ThenBy(m => m.Brand)
            .ThenBy(m => m.ModelName)
            .ToListAsync();

        return Ok(list.Select(ToSummaryDto));
    }

    [HttpGet("vehicle-models/{id:guid}")]
    public async Task<ActionResult<PublicVehicleModelDetailDto>> GetVehicleModelById(Guid id)
    {
        var item = await PublishedModels()
            .Include(m => m.Category)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (item == null)
            return NotFound(new { message = "Vehicle model not found" });

        return Ok(ToDetailDto(item));
    }

    private IQueryable<VehicleModel> PublishedModels() =>
        _db.VehicleModels
            .AsNoTracking()
            .Where(m => m.IsActive && m.IsPublished);

    private static string? FormatDrivetrain(Drivetrain? value) =>
        value switch
        {
            null => null,
            Drivetrain.FourWD => "4WD",
            _ => value.ToString(),
        };

    private static PublicVehicleModelSummaryDto ToSummaryDto(VehicleModel m) => new()
    {
        Id = m.Id,
        Brand = m.Brand,
        ModelName = m.ModelName,
        VariantName = m.VariantName,
        ManufactureYear = m.ManufactureYear,
        CategoryId = m.CategoryId,
        CategoryName = m.Category?.Name ?? string.Empty,
        SeatCount = m.SeatCount,
        TransmissionType = m.TransmissionType.ToString(),
        FuelType = m.FuelType.ToString(),
        BaseDailyPrice = m.BaseDailyPrice,
        DepositAmount = m.DepositAmount,
        ThumbnailUrl = m.ThumbnailUrl,
    };

    private static PublicVehicleModelDetailDto ToDetailDto(VehicleModel m) => new()
    {
        Id = m.Id,
        Brand = m.Brand,
        ModelName = m.ModelName,
        VariantName = m.VariantName,
        ManufactureYear = m.ManufactureYear,
        CategoryId = m.CategoryId,
        CategoryName = m.Category?.Name ?? string.Empty,
        SeatCount = m.SeatCount,
        DoorCount = m.DoorCount,
        TransmissionType = m.TransmissionType.ToString(),
        FuelType = m.FuelType.ToString(),
        EngineCapacity = m.EngineCapacity,
        Drivetrain = FormatDrivetrain(m.Drivetrain),
        LuggageCapacity = m.LuggageCapacity,
        FuelConsumption = m.FuelConsumption,
        BaseDailyPrice = m.BaseDailyPrice,
        WeekendDailyPrice = m.WeekendDailyPrice,
        HolidayDailyPrice = m.HolidayDailyPrice,
        DepositAmount = m.DepositAmount,
        IncludedKmPerDay = m.IncludedKmPerDay,
        ExtraKmPrice = m.ExtraKmPrice,
        LateHourPrice = m.LateHourPrice,
        ThumbnailUrl = m.ThumbnailUrl,
        Description = m.Description,
    };
}
