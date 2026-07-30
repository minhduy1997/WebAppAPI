using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebAppApi.Data;
using WebAppApi.Models;
using WebAppApi.Services;

namespace WebAppApi.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/[controller]")]
public class PermissionsController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly PermissionService _permissionService;

    public PermissionsController(
        ApplicationDbContext db,
        RoleManager<IdentityRole> roleManager,
        UserManager<ApplicationUser> userManager,
        PermissionService permissionService)
    {
        _db = db;
        _roleManager = roleManager;
        _userManager = userManager;
        _permissionService = permissionService;
    }

    /// <summary>Matrix: each role with assigned page IDs.</summary>
    [HttpGet("role-pages")]
    public async Task<ActionResult<IEnumerable<RolePagePermissionDto>>> GetRolePages()
    {
        var roles = await _roleManager.Roles.OrderBy(r => r.Name).ToListAsync();
        var allPerms = await _db.RolePagePermissions.ToListAsync();

        var result = roles.Select(role => new RolePagePermissionDto
        {
            RoleId = role.Id,
            RoleName = role.Name!,
            PageIds = allPerms.Where(p => p.RoleId == role.Id).Select(p => p.PageId).ToList()
        }).ToList();

        return Ok(result);
    }

    [HttpPut("role-pages")]
    public async Task<IActionResult> SetRolePages([FromBody] SetRolePagesRequest request)
    {
        var role = await _roleManager.FindByIdAsync(request.RoleId);
        if (role == null)
            return NotFound(new { message = "Role not found" });

        var validPageIds = await _db.AppPages
            .Where(p => request.PageIds.Contains(p.Id))
            .Select(p => p.Id)
            .ToListAsync();

        var existing = await _db.RolePagePermissions
            .Where(p => p.RoleId == request.RoleId)
            .ToListAsync();

        _db.RolePagePermissions.RemoveRange(existing);

        foreach (var pageId in validPageIds.Distinct())
        {
            _db.RolePagePermissions.Add(new RolePagePermission
            {
                RoleId = request.RoleId,
                PageId = pageId
            });
        }

        await _db.SaveChangesAsync();
        return Ok(new { message = "Permissions updated", roleId = request.RoleId, pageIds = validPageIds });
    }

    /// <summary>Effective pages for a specific user (via their roles).</summary>
    [HttpGet("users/{userId}/pages")]
    public async Task<ActionResult<IEnumerable<PageDto>>> GetUserPages(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return NotFound(new { message = "User not found" });

        var pages = await _permissionService.GetAllowedPagesAsync(user);
        return Ok(pages.Select(p => new PageDto
        {
            Id = p.Id,
            Name = p.Name,
            Path = p.Path,
            Description = p.Description,
            SortOrder = p.SortOrder,
            IsActive = p.IsActive
        }));
    }
}
