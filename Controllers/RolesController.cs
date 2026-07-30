using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebAppApi.Data;
using WebAppApi.Models;

namespace WebAppApi.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/[controller]")]
public class RolesController : ControllerBase
{
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _db;

    public RolesController(
        RoleManager<IdentityRole> roleManager,
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext db)
    {
        _roleManager = roleManager;
        _userManager = userManager;
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<RoleDto>>> GetRoles()
    {
        var roles = await _roleManager.Roles.OrderBy(r => r.Name).ToListAsync();
        var result = new List<RoleDto>();

        foreach (var role in roles)
        {
            var usersInRole = await _userManager.GetUsersInRoleAsync(role.Name!);
            result.Add(new RoleDto
            {
                Id = role.Id,
                Name = role.Name!,
                UserCount = usersInRole.Count
            });
        }

        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<RoleDto>> CreateRole([FromBody] CreateRoleRequest request)
    {
        var name = request.Name.Trim();
        if (await _roleManager.RoleExistsAsync(name))
            return BadRequest(new { message = "Role already exists" });

        var role = new IdentityRole(name);
        var result = await _roleManager.CreateAsync(role);
        if (!result.Succeeded)
            return BadRequest(result.Errors);

        return Ok(new RoleDto { Id = role.Id, Name = role.Name!, UserCount = 0 });
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<RoleDto>> UpdateRole(string id, [FromBody] UpdateRoleRequest request)
    {
        var role = await _roleManager.FindByIdAsync(id);
        if (role == null)
            return NotFound(new { message = "Role not found" });

        var name = request.Name.Trim();
        if (!string.Equals(role.Name, name, StringComparison.OrdinalIgnoreCase)
            && await _roleManager.RoleExistsAsync(name))
        {
            return BadRequest(new { message = "Role name already exists" });
        }

        role.Name = name;
        role.NormalizedName = name.ToUpperInvariant();
        var result = await _roleManager.UpdateAsync(role);
        if (!result.Succeeded)
            return BadRequest(result.Errors);

        var usersInRole = await _userManager.GetUsersInRoleAsync(role.Name!);
        return Ok(new RoleDto { Id = role.Id, Name = role.Name!, UserCount = usersInRole.Count });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteRole(string id)
    {
        var role = await _roleManager.FindByIdAsync(id);
        if (role == null)
            return NotFound(new { message = "Role not found" });

        if (role.Name is AppRoles.Admin or AppRoles.Staff or AppRoles.Customer or AppRoles.LegacyUser)
            return BadRequest(new { message = "Cannot delete system roles Admin/Staff/Customer" });

        var usersInRole = await _userManager.GetUsersInRoleAsync(role.Name!);
        foreach (var user in usersInRole)
            await _userManager.RemoveFromRoleAsync(user, role.Name!);

        var perms = await _db.RolePagePermissions.Where(p => p.RoleId == id).ToListAsync();
        _db.RolePagePermissions.RemoveRange(perms);
        await _db.SaveChangesAsync();

        var result = await _roleManager.DeleteAsync(role);
        if (!result.Succeeded)
            return BadRequest(result.Errors);

        return Ok(new { message = "Role deleted" });
    }

    [HttpPut("users/{userId}/roles")]
    public async Task<IActionResult> SetUserRoles(string userId, [FromBody] SetUserRolesRequest request)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return NotFound(new { message = "User not found" });

        var currentRoles = await _userManager.GetRolesAsync(user);
        var targetRoles = request.Roles.Distinct().ToList();

        var toRemove = currentRoles.Except(targetRoles).ToList();
        var toAdd = targetRoles.Except(currentRoles).ToList();

        if (toRemove.Count > 0)
            await _userManager.RemoveFromRolesAsync(user, toRemove);

        foreach (var roleName in toAdd)
        {
            if (!await _roleManager.RoleExistsAsync(roleName))
                return BadRequest(new { message = $"Role '{roleName}' does not exist" });
            await _userManager.AddToRoleAsync(user, roleName);
        }

        return Ok(new
        {
            user.Id,
            user.Email,
            Roles = await _userManager.GetRolesAsync(user)
        });
    }
}
