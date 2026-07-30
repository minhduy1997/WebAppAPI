using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebAppApi.Data;
using WebAppApi.Models;

namespace WebAppApi.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/[controller]")]
public class VehicleCategoriesController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public VehicleCategoriesController(ApplicationDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<VehicleCategoriesDto>>> GetAll()
    {
        var list = await _db.VehicleCategories
            .OrderBy(x => x.Name)
            .ToListAsync();

        return Ok(list.Select(ToDto));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<VehicleCategoriesDto>> GetById(int id)
    {
        var item = await _db.VehicleCategories.FindAsync(id);
        if (item == null)
            return NotFound(new { message = "Vehicle category not found" });

        return Ok(ToDto(item));
    }

    [HttpPost]
    public async Task<ActionResult<VehicleCategoriesDto>> Create([FromBody] CreateVehicleCategoriesRequest request)
    {
        var name = request.Name.Trim();
        if (await _db.VehicleCategories.AnyAsync(x => x.Name == name))
            return BadRequest(new { message = "Vehicle category already exists" });

        var item = new VehicleCategories
        {
            Name = name,
            Description = request.Description.Trim(),
            IsActive = request.IsActive,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        _db.VehicleCategories.Add(item);
        await _db.SaveChangesAsync();
        return Ok(ToDto(item));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<VehicleCategoriesDto>> Update(int id, [FromBody] UpdateVehicleCategoryRequest request)
    {
        var item = await _db.VehicleCategories.FindAsync(id);
        if (item == null)
            return NotFound(new { message = "Vehicle category not found" });

        var name = request.Name.Trim();
        if (!string.Equals(item.Name, name, StringComparison.OrdinalIgnoreCase)
            && await _db.VehicleCategories.AnyAsync(x => x.Name == name))
        {
            return BadRequest(new { message = "Vehicle category already exists" });
        }

        item.Name = name;
        item.Description = request.Description.Trim();
        item.IsActive = request.IsActive;
        item.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return Ok(ToDto(item));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await _db.VehicleCategories.FindAsync(id);
        if (item == null)
            return NotFound(new { message = "Vehicle category not found" });

        _db.VehicleCategories.Remove(item);
        await _db.SaveChangesAsync();
        return Ok(new { message = "Vehicle category deleted" });
    }

    private static VehicleCategoriesDto ToDto(VehicleCategories entity) => new()
    {
        Id = entity.Id,
        Name = entity.Name,
        Description = entity.Description,
        IsActive = entity.IsActive,
    };
}
