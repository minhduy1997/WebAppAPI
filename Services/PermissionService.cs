using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WebAppApi.Data;
using WebAppApi.Models;

namespace WebAppApi.Services;

public class PermissionService
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public PermissionService(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    public async Task<IList<string>> GetAllowedPagePathsAsync(ApplicationUser user)
    {
        var roles = await _userManager.GetRolesAsync(user);
        if (roles.Count == 0)
            return new List<string>();

        var roleIds = await _db.Roles
            .Where(r => roles.Contains(r.Name!))
            .Select(r => r.Id)
            .ToListAsync();

        return await _db.RolePagePermissions
            .Where(p => roleIds.Contains(p.RoleId))
            .Select(p => p.Page!)
            .Where(p => p.IsActive)
            .OrderBy(p => p.SortOrder)
            .Select(p => p.Path)
            .Distinct()
            .ToListAsync();
    }

    public async Task<IList<AppPage>> GetAllowedPagesAsync(ApplicationUser user)
    {
        var roles = await _userManager.GetRolesAsync(user);
        if (roles.Count == 0)
            return new List<AppPage>();

        var roleIds = await _db.Roles
            .Where(r => roles.Contains(r.Name!))
            .Select(r => r.Id)
            .ToListAsync();

        var pageIds = await _db.RolePagePermissions
            .Where(p => roleIds.Contains(p.RoleId))
            .Select(p => p.PageId)
            .Distinct()
            .ToListAsync();

        return await _db.AppPages
            .Where(p => pageIds.Contains(p.Id) && p.IsActive)
            .OrderBy(p => p.SortOrder)
            .ToListAsync();
    }
}
