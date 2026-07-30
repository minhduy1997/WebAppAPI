using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebAppApi.Data;
using WebAppApi.Models;
using WebAppApi.Services;

namespace WebAppApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PagesController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly PermissionService _permissionService;

    public PagesController(
        ApplicationDbContext db,
        UserManager<ApplicationUser> userManager,
        PermissionService permissionService)
    {
        _db = db;
        _userManager = userManager;
        _permissionService = permissionService;
    }

    /// <summary>Pages the current user is allowed to access (for sidebar).</summary>
    [Authorize]
    [HttpGet("mine")]
    public async Task<ActionResult<IEnumerable<PageDto>>> GetMyPages()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var user = await _userManager.FindByIdAsync(userId!);
        if (user == null)
            return NotFound();

        var pages = await _permissionService.GetAllowedPagesAsync(user);
        return Ok(pages.Select(ToDto));
    }

    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<ActionResult<IEnumerable<PageDto>>> GetPages()
    {
        var pages = await _db.AppPages
            .OrderBy(p => p.SortOrder)
            .ThenBy(p => p.Name)
            .Select(p => new PageDto
            {
                Id = p.Id,
                Name = p.Name,
                Path = p.Path,
                Description = p.Description,
                SortOrder = p.SortOrder,
                IsActive = p.IsActive
            })
            .ToListAsync();

        return Ok(pages);
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("{id:int}")]
    public async Task<ActionResult<PageDto>> GetPage(int id)
    {
        var page = await _db.AppPages.FindAsync(id);
        if (page == null)
            return NotFound(new { message = "Page not found" });

        return Ok(ToDto(page));
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<ActionResult<PageDto>> CreatePage([FromBody] CreatePageRequest request)
    {
        var path = NormalizePath(request.Path);
        if (await _db.AppPages.AnyAsync(p => p.Path == path))
            return BadRequest(new { message = "Page path already exists" });

        var page = new AppPage
        {
            Name = request.Name.Trim(),
            Path = path,
            Description = request.Description?.Trim(),
            SortOrder = request.SortOrder,
            IsActive = request.IsActive
        };

        _db.AppPages.Add(page);
        await _db.SaveChangesAsync();

        return Ok(ToDto(page));
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("{id:int}")]
    public async Task<ActionResult<PageDto>> UpdatePage(int id, [FromBody] UpdatePageRequest request)
    {
        var page = await _db.AppPages.FindAsync(id);
        if (page == null)
            return NotFound(new { message = "Page not found" });

        var path = NormalizePath(request.Path);
        if (await _db.AppPages.AnyAsync(p => p.Path == path && p.Id != id))
            return BadRequest(new { message = "Page path already exists" });

        page.Name = request.Name.Trim();
        page.Path = path;
        page.Description = request.Description?.Trim();
        page.SortOrder = request.SortOrder;
        page.IsActive = request.IsActive;

        await _db.SaveChangesAsync();
        return Ok(ToDto(page));
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeletePage(int id)
    {
        var page = await _db.AppPages.FindAsync(id);
        if (page == null)
            return NotFound(new { message = "Page not found" });

        _db.AppPages.Remove(page);
        await _db.SaveChangesAsync();
        return Ok(new { message = "Page deleted" });
    }

    private static string NormalizePath(string path)
    {
        path = path.Trim();
        if (!path.StartsWith('/'))
            path = "/" + path;
        return path;
    }

    private static PageDto ToDto(AppPage page) => new()
    {
        Id = page.Id,
        Name = page.Name,
        Path = page.Path,
        Description = page.Description,
        SortOrder = page.SortOrder,
        IsActive = page.IsActive
    };
}
