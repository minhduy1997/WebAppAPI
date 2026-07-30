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
public class AuthController : ControllerBase
{
    private static readonly HashSet<string> AllowedAvatarExtensions =
        new(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".gif", ".webp" };

    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly TokenService _tokenService;
    private readonly PermissionService _permissionService;
    private readonly ApplicationDbContext _db;
    private readonly IWebHostEnvironment _env;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        TokenService tokenService,
        PermissionService permissionService,
        ApplicationDbContext db,
        IWebHostEnvironment env)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _tokenService = tokenService;
        _permissionService = permissionService;
        _db = db;
        _env = env;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        return await RegisterCustomer(request);
    }

    /// <summary>
    /// Customer web signup:
    /// AspNetUser → role Customer → CustomerProfile (verification NOT_SUBMITTED).
    /// </summary>
    [HttpPost("register-customer")]
    public async Task<IActionResult> RegisterCustomer([FromBody] RegisterRequest request)
    {
        if (!string.Equals(request.Password, request.ConfirmPassword, StringComparison.Ordinal))
            return BadRequest(new { message = "Password and confirm password do not match" });

        var email = request.Email.Trim();
        var fullName = request.FullName.Trim();
        var phone = NormalizePhone(request.PhoneNumber);

        if (string.IsNullOrWhiteSpace(fullName))
            return BadRequest(new { message = "Full name is required" });

        if (string.IsNullOrWhiteSpace(phone))
            return BadRequest(new { message = "Phone number is required" });

        var existing = await _userManager.FindByEmailAsync(email);
        if (existing != null)
            return BadRequest(new { message = "Email already registered" });

        if (await _db.CustomerProfiles.AnyAsync(x => x.PhoneNumber == phone))
            return BadRequest(new { message = "Phone number already registered" });

        if (!await _roleManager.RoleExistsAsync(AppRoles.Customer))
            await _roleManager.CreateAsync(new IdentityRole(AppRoles.Customer));

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            PhoneNumber = phone,
            FullName = fullName,
            IsActive = true
        };

        await using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            var result = await _userManager.CreateAsync(user, request.Password);
            if (!result.Succeeded)
                return BadRequest(result.Errors);

            await _userManager.AddToRoleAsync(user, AppRoles.Customer);

            var profile = new CustomerProfile
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                CustomerCode = await GenerateCustomerCodeAsync(),
                FullName = fullName,
                PhoneNumber = phone,
                Email = email,
                VerificationStatus = CustomerVerificationStatus.NOT_SUBMITTED,
                CreatedAt = DateTime.UtcNow,
            };

            _db.CustomerProfiles.Add(profile);
            await _db.SaveChangesAsync();
            await tx.CommitAsync();
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }

        var (token, expiration) = await _tokenService.GenerateTokenAsync(user);
        var roles = await _userManager.GetRolesAsync(user);
        var allowedPages = await _permissionService.GetAllowedPagePathsAsync(user);

        return Ok(await ToAuthResponseAsync(user, token, expiration, roles, allowedPages));
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null || !await _userManager.CheckPasswordAsync(user, request.Password))
            return Unauthorized(new { message = "Invalid email or password" });

        if (!user.IsActive)
            return Unauthorized(new { message = "Account is inactive" });

        var (token, expiration) = await _tokenService.GenerateTokenAsync(user);
        var roles = await _userManager.GetRolesAsync(user);
        var allowedPages = await _permissionService.GetAllowedPagePathsAsync(user);

        return Ok(await ToAuthResponseAsync(user, token, expiration, roles, allowedPages));
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("assign-role")]
    public async Task<IActionResult> AssignRole([FromBody] AssignRoleRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null)
            return NotFound(new { message = "User not found" });

        if (!await _roleManager.RoleExistsAsync(request.Role))
            await _roleManager.CreateAsync(new IdentityRole(request.Role));

        var result = await _userManager.AddToRoleAsync(user, request.Role);
        if (!result.Succeeded)
            return BadRequest(result.Errors);

        return Ok(new { message = $"Role '{request.Role}' assigned to '{request.Email}'" });
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> GetCurrentUser()
    {
        var user = await GetCurrentApplicationUserAsync();
        if (user == null)
            return NotFound();

        var roles = await _userManager.GetRolesAsync(user);
        var allowedPages = await _permissionService.GetAllowedPagePathsAsync(user);
        var response = await ToAuthResponseAsync(user, token: string.Empty, expiration: default, roles, allowedPages);
        return Ok(new
        {
            user.Email,
            user.UserName,
            user.FullName,
            user.AvatarUrl,
            user.IsActive,
            Roles = roles,
            AllowedPages = allowedPages,
            response.CustomerProfileId,
            response.CustomerCode,
            response.VerificationStatus
        });
    }

    [Authorize]
    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
    {
        var user = await GetCurrentApplicationUserAsync();
        if (user == null)
            return NotFound(new { message = "User not found" });

        var newEmail = request.Email.Trim();
        var newFullName = request.FullName.Trim();

        if (!string.Equals(user.Email, newEmail, StringComparison.OrdinalIgnoreCase))
        {
            var existing = await _userManager.FindByEmailAsync(newEmail);
            if (existing != null && existing.Id != user.Id)
                return BadRequest(new { message = "Email already in use" });

            var emailResult = await _userManager.SetEmailAsync(user, newEmail);
            if (!emailResult.Succeeded)
                return BadRequest(emailResult.Errors);

            var userNameResult = await _userManager.SetUserNameAsync(user, newEmail);
            if (!userNameResult.Succeeded)
                return BadRequest(userNameResult.Errors);
        }

        user.FullName = newFullName;
        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
            return BadRequest(updateResult.Errors);

        var (token, expiration) = await _tokenService.GenerateTokenAsync(user);
        var roles = await _userManager.GetRolesAsync(user);
        var allowedPages = await _permissionService.GetAllowedPagePathsAsync(user);

        return Ok(await ToAuthResponseAsync(user, token, expiration, roles, allowedPages));
    }

    [Authorize]
    [HttpPost("avatar")]
    [RequestSizeLimit(5 * 1024 * 1024)]
    public async Task<IActionResult> UploadAvatar(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { message = "No file uploaded" });

        var extension = Path.GetExtension(file.FileName);
        if (string.IsNullOrWhiteSpace(extension) || !AllowedAvatarExtensions.Contains(extension))
            return BadRequest(new { message = "Only image files are allowed (.jpg, .jpeg, .png, .gif, .webp)" });

        if (file.Length > 5 * 1024 * 1024)
            return BadRequest(new { message = "File size must be 5MB or less" });

        var user = await GetCurrentApplicationUserAsync();
        if (user == null)
            return NotFound(new { message = "User not found" });

        var uploadsFolder = Path.Combine(_env.ContentRootPath, "wwwroot", "uploads", "avatars");
        Directory.CreateDirectory(uploadsFolder);

        var originalName = Path.GetFileNameWithoutExtension(file.FileName);
        foreach (var c in Path.GetInvalidFileNameChars())
            originalName = originalName.Replace(c, '_');
        if (string.IsNullOrWhiteSpace(originalName))
            originalName = "avatar";

        var storedFileName = $"{originalName}_{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
        var physicalPath = Path.Combine(uploadsFolder, storedFileName);

        await using (var stream = new FileStream(physicalPath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        if (!string.IsNullOrWhiteSpace(user.AvatarUrl) &&
            user.AvatarUrl.StartsWith("/uploads/avatars/", StringComparison.OrdinalIgnoreCase))
        {
            var oldPath = Path.Combine(_env.ContentRootPath, "wwwroot",
                user.AvatarUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
            if (System.IO.File.Exists(oldPath))
            {
                try { System.IO.File.Delete(oldPath); } catch { /* ignore */ }
            }
        }

        user.AvatarUrl = $"/uploads/avatars/{storedFileName}";
        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
            return BadRequest(updateResult.Errors);

        var (token, expiration) = await _tokenService.GenerateTokenAsync(user);
        var roles = await _userManager.GetRolesAsync(user);
        var allowedPages = await _permissionService.GetAllowedPagePathsAsync(user);

        return Ok(await ToAuthResponseAsync(user, token, expiration, roles, allowedPages));
    }

    [Authorize]
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        var user = await GetCurrentApplicationUserAsync();
        if (user == null)
            return NotFound(new { message = "User not found" });

        var result = await _userManager.ChangePasswordAsync(
            user,
            request.CurrentPassword,
            request.NewPassword);

        if (!result.Succeeded)
            return BadRequest(result.Errors);

        return Ok(new { message = "Password changed successfully" });
    }

    private async Task<string> GenerateCustomerCodeAsync()
    {
        var prefix = $"CUS{DateTime.UtcNow:yyMMdd}";
        var existing = await _db.CustomerProfiles
            .Where(x => x.CustomerCode.StartsWith(prefix))
            .Select(x => x.CustomerCode)
            .ToListAsync();

        var next = 1;
        foreach (var code in existing)
        {
            var suffix = code[prefix.Length..];
            if (int.TryParse(suffix, out var n) && n >= next)
                next = n + 1;
        }

        return $"{prefix}{next:D3}";
    }

    private static string NormalizePhone(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var cleaned = new string(value.Where(c => char.IsDigit(c) || c == '+').ToArray());
        return cleaned;
    }

    private async Task<ApplicationUser?> GetCurrentApplicationUserAsync()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return null;

        return await _userManager.FindByIdAsync(userId);
    }

    private async Task<AuthResponse> ToAuthResponseAsync(
        ApplicationUser user,
        string token,
        DateTime expiration,
        IList<string> roles,
        IList<string>? allowedPages = null)
    {
        var response = new AuthResponse
        {
            Token = token,
            Expiration = expiration,
            Email = user.Email!,
            FullName = user.FullName,
            AvatarUrl = user.AvatarUrl,
            Roles = roles,
            AllowedPages = allowedPages ?? new List<string>()
        };

        if (roles.Contains(AppRoles.Customer))
        {
            var profile = await _db.CustomerProfiles
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.UserId == user.Id);
            if (profile != null)
            {
                response.CustomerProfileId = profile.Id;
                response.CustomerCode = profile.CustomerCode;
                response.VerificationStatus = profile.VerificationStatus.ToString();
            }
        }

        return response;
    }
}
